using FluentValidation;
using IranJob.Modules.EmployerProfile.Domain.Entities;
using IranJob.Modules.EmployerProfile.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Application.Abstractions;
using IranJob.Modules.JobPostings.Application.Contracts;
using IranJob.Modules.JobPostings.Domain.Entities;
using IranJob.Modules.JobPostings.Domain.Enums;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.Payments.Application.Abstractions;
using IranJob.Modules.Payments.Application.Contracts;
using IranJob.Modules.ReferenceData.Domain.Entities;
using IranJob.Modules.ReferenceData.Infrastructure.Persistence;
using IranJob.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = IranJob.SharedKernel.Exceptions.ValidationException;

namespace IranJob.Modules.JobPostings.Infrastructure.Services;

public sealed class JobPostingService(
    JobPostingDbContext jobDbContext,
    EmployerDbContext employerDbContext,
    ReferenceDataDbContext referenceDataDbContext,
    IPaymentService paymentService,
    IValidator<CreateJobPostingRequest> createValidator,
    IValidator<UpdateJobPostingRequest> updateValidator) : IJobPostingService
{
    public async Task<IReadOnlyList<JobPostingResponse>> GetOwnAsync(Guid userId, CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(userId, cancellationToken);
        var postings = await jobDbContext.JobPostings.AsNoTracking()
            .Include(posting => posting.Skills)
            .Where(posting => posting.EmployerProfileId == employerProfileId)
            .ToListAsync(cancellationToken);
        return await MapAsync(postings.OrderByDescending(posting => posting.CreatedAt).ToArray(), cancellationToken);
    }

    public async Task<JobPostingResponse> GetOwnByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(userId, cancellationToken);
        var posting = await GetPostingAsync(employerProfileId, id, cancellationToken, asNoTracking: true);
        return (await MapAsync([posting], cancellationToken))[0];
    }

    public async Task<JobPostingResponse> CreateAsync(Guid userId, CreateJobPostingRequest request, CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(userId, cancellationToken);
        await ValidateAsync(createValidator, request, cancellationToken);
        await ValidateReferencesAsync(request.CategoryId, request.SkillIds, request.CityId, cancellationToken);

        var posting = JobPosting.Create(employerProfileId, ToDetails(request));
        jobDbContext.JobPostings.Add(posting);
        await jobDbContext.SaveChangesAsync(cancellationToken);
        return (await MapAsync([posting], cancellationToken))[0];
    }

    public async Task<JobPostingResponse> UpdateAsync(Guid userId, Guid id, UpdateJobPostingRequest request, CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(userId, cancellationToken);
        await ValidateAsync(updateValidator, request, cancellationToken);
        var posting = await GetPostingAsync(employerProfileId, id, cancellationToken);
        await ValidateReferencesAsync(request.CategoryId, request.SkillIds, request.CityId, cancellationToken);
        posting.Update(ToDetails(request));
        await jobDbContext.SaveChangesAsync(cancellationToken);
        return (await MapAsync([posting], cancellationToken))[0];
    }

        public async Task<JobPostingResponse> PublishAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        {
            var employerProfileId = await GetEmployerProfileIdAsync(userId, cancellationToken);
            var posting = await GetPostingAsync(employerProfileId, id, cancellationToken);
            await ValidateReferencesAsync(posting.CategoryId, posting.Skills.Select(skill => skill.SkillId).ToArray(), posting.CityId, cancellationToken);

            // Fee-gated publishing. With PublishFeeAmount == 0 (default/test path)
            // the payment completes in-process and the job is published immediately,
            // preserving the existing fee-free behavior. When a fee applies the job
            // stays Draft until the payer completes the gateway; the PaymentCompleted
            // handler publishes it.
            var initiation = await paymentService.InitiateForJobPublishAsync(employerProfileId, id, cancellationToken);
            if (initiation.IsCompleted)
            {
                // When fee == 0 the PaymentCompleted handler may have already
                // published the job via PublishByIdAsync.  If so, skip the
                // redundant Publish() call.  For any other non-Published status
                // (e.g. Closed), fall through to posting.Publish() so the
                // appropriate DomainException is thrown and mapped to 409.
                if (posting.Status != JobPostingStatus.Published)
                    posting.Publish();
                await jobDbContext.SaveChangesAsync(cancellationToken);
                return (await MapAsync([posting], cancellationToken))[0] with { PaymentInfo = ToPaymentInfo(initiation) };
            }

            return (await MapAsync([posting], cancellationToken))[0] with
            {
                PaymentInfo = ToPaymentInfo(initiation)
            };
        }

    public async Task<JobPostingResponse> UnpublishAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(userId, cancellationToken);
        var posting = await GetPostingAsync(employerProfileId, id, cancellationToken);
        posting.Unpublish();
        await jobDbContext.SaveChangesAsync(cancellationToken);
        return (await MapAsync([posting], cancellationToken))[0];
    }

    public async Task<JobPostingResponse> CloseAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(userId, cancellationToken);
        var posting = await GetPostingAsync(employerProfileId, id, cancellationToken);
        posting.Close();
        await jobDbContext.SaveChangesAsync(cancellationToken);
        return (await MapAsync([posting], cancellationToken))[0];
    }

    /// <summary>
    /// Publishes a job posting by id (used by the Payments module's
    /// PaymentCompleted handler after a publish fee is verified).
    /// </summary>
    public async Task<JobPostingResponse> PublishByIdAsync(Guid jobPostingId, CancellationToken cancellationToken)
    {
        var posting = await jobDbContext.JobPostings
            .Include(posting => posting.Skills)
            .SingleOrDefaultAsync(posting => posting.Id == jobPostingId, cancellationToken)
            ?? throw new NotFoundException("Job posting was not found.");

        posting.Publish();
        await jobDbContext.SaveChangesAsync(cancellationToken);
        return (await MapAsync([posting], cancellationToken))[0];
    }

    private static PaymentInfo? ToPaymentInfo(PaymentInitiationResponse initiation) =>
        initiation.IsCompleted
            ? null
                        : new PaymentInfo(initiation.PaymentId, initiation.Authority, initiation.RedirectUrl,
                initiation.Amount, initiation.Currency, initiation.Status, initiation.PaymentMethod, false);

    private async Task<Guid> GetEmployerProfileIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            throw new UnauthorizedException();

        return await employerDbContext.EmployerProfiles.AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => profile.Id)
            .SingleOrDefaultAsync(cancellationToken) is var profileId && profileId != Guid.Empty
            ? profileId
            : throw new NotFoundException("An employer profile is required to manage job postings.");
    }

    private async Task<JobPosting> GetPostingAsync(
        Guid employerProfileId,
        Guid id,
        CancellationToken cancellationToken,
        bool asNoTracking = false)
    {
        IQueryable<JobPosting> query = jobDbContext.JobPostings.Include(posting => posting.Skills);
        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.SingleOrDefaultAsync(
            posting => posting.Id == id && posting.EmployerProfileId == employerProfileId,
            cancellationToken)
            ?? throw new NotFoundException("Job posting was not found.");
    }

    private async Task ValidateReferencesAsync(
        Guid categoryId,
        IReadOnlyCollection<Guid> skillIds,
        Guid? cityId,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (!await referenceDataDbContext.JobCategories.AnyAsync(category => category.Id == categoryId && category.IsActive, cancellationToken))
            errors[nameof(JobPostingInput.CategoryId)] = ["An active job category is required."];

        var requestedSkillIds = skillIds.Distinct().ToArray();
        if (requestedSkillIds.Length > 0)
        {
            var activeSkillCount = await referenceDataDbContext.Skills.CountAsync(
                skill => requestedSkillIds.Contains(skill.Id) && skill.IsActive, cancellationToken);
            if (activeSkillCount != requestedSkillIds.Length)
                errors[nameof(JobPostingInput.SkillIds)] = ["Every selected skill must exist and be active."];
        }

        if (cityId.HasValue)
        {
            var cityIsActive = await referenceDataDbContext.Cities.AnyAsync(
                city => city.Id == cityId.Value && city.IsActive && city.Province.IsActive && city.Province.Country.IsActive,
                cancellationToken);
            if (!cityIsActive)
                errors[nameof(JobPostingInput.CityId)] = ["The selected city and its province and country must be active."];
        }

        if (errors.Count > 0)
            throw new DomainValidationException(errors);
    }

    private async Task<IReadOnlyList<JobPostingResponse>> MapAsync(
        IReadOnlyCollection<JobPosting> postings,
        CancellationToken cancellationToken)
    {
        if (postings.Count == 0)
            return Array.Empty<JobPostingResponse>();

        var categoryIds = postings.Select(posting => posting.CategoryId).Distinct().ToArray();
        var skillIds = postings.SelectMany(posting => posting.Skills).Select(skill => skill.SkillId).Distinct().ToArray();
        var cityIds = postings.Where(posting => posting.CityId.HasValue).Select(posting => posting.CityId!.Value).Distinct().ToArray();

        var categories = await referenceDataDbContext.JobCategories.AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id)).ToDictionaryAsync(category => category.Id, cancellationToken);
        var skills = skillIds.Length == 0
            ? new Dictionary<Guid, Skill>()
            : await referenceDataDbContext.Skills.AsNoTracking().Where(skill => skillIds.Contains(skill.Id))
                .ToDictionaryAsync(skill => skill.Id, cancellationToken);
        var cities = cityIds.Length == 0
            ? new Dictionary<Guid, City>()
            : await referenceDataDbContext.Cities.AsNoTracking().Include(city => city.Province).ThenInclude(province => province.Country)
                .Where(city => cityIds.Contains(city.Id)).ToDictionaryAsync(city => city.Id, cancellationToken);

        return postings.Select(posting =>
        {
            cities.TryGetValue(posting.CityId ?? Guid.Empty, out var city);
            return new JobPostingResponse(
                posting.Id,
                posting.Title,
                posting.Description,
                posting.CategoryId,
                categories[posting.CategoryId].Name,
                posting.Skills.Select(link => skills.TryGetValue(link.SkillId, out var skill)
                        ? new JobPostingReference(skill.Id, skill.Name)
                        : null)
                    .Where(skill => skill is not null)
                    .Select(skill => skill!)
                    .OrderBy(skill => skill.Name)
                    .ToArray(),
                posting.EmploymentType.ToString(),
                posting.WorkArrangement.ToString(),
                posting.CityId,
                city?.Name,
                city?.Province.Name,
                city?.Province.Country.Name,
                posting.SalaryMinimum,
                posting.SalaryMaximum,
                posting.SalaryCurrency,
                posting.SalaryPeriod?.ToString(),
                posting.Status.ToString(),
                posting.CreatedAt,
                posting.UpdatedAt,
                posting.PublishedAt,
                posting.ClosedAt,
                posting.ClosingAt);
        }).ToArray();
    }

    private static JobPostingDetails ToDetails(JobPostingInput input) => new(
        input.Title,
        input.Description,
        input.CategoryId,
        input.SkillIds,
        Enum.Parse<IranJob.Modules.JobPostings.Domain.Enums.EmploymentType>(input.EmploymentType, ignoreCase: false),
        Enum.Parse<IranJob.Modules.JobPostings.Domain.Enums.WorkArrangement>(input.WorkArrangement, ignoreCase: false),
        input.CityId,
        input.SalaryMinimum,
        input.SalaryMaximum,
        input.SalaryCurrency,
        string.IsNullOrWhiteSpace(input.SalaryPeriod)
            ? null
            : Enum.Parse<SalaryPeriod>(input.SalaryPeriod, ignoreCase: false),
        input.ClosingAt);

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new DomainValidationException(result.Errors.GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
    }
}

