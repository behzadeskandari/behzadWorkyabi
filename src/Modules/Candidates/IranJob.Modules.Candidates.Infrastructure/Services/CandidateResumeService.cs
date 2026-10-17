using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using IranJob.BuildingBlocks.Infrastructure.Configuration;
using IranJob.Modules.Candidates.Application.Abstractions;
using IranJob.Modules.Candidates.Application.Contracts;
using IranJob.Modules.Candidates.Application.Validators;
using IranJob.Modules.Candidates.Domain.Entities;
using IranJob.Modules.Candidates.Domain.Enums;
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DomainValidationException = IranJob.SharedKernel.Exceptions.ValidationException;

namespace IranJob.Modules.Candidates.Infrastructure.Services;

public sealed class CandidateResumeService(
    CandidateDbContext dbContext,
    IValidator<SaveResumeRequest> requestValidator,
    IValidator<CvUploadRequest> cvUploadValidator,
    IOptions<FileStorageOptions> fileStorageOptions) : ICandidateResumeService
{
    public async Task<ResumeResult> GetResumeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var resume = await LoadAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Candidate resume was not found.");
        return MapToResult(resume);
    }

    public async Task<ResumeResult> UpsertResumeAsync(
        Guid userId,
        SaveResumeRequest request,
        IEnumerable<SkillReference> skillReferences,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);
        var skillRefs = skillReferences?.ToDictionary(r => r.Id, r => r.Name) ?? new Dictionary<Guid, string>();

        var resume = await LoadAsync(userId, cancellationToken);
        if (resume is null)
        {
            resume = CandidateResume.Create(userId, request.ProfessionalSummary);
            dbContext.CandidateResumes.Add(resume);
        }
        else
        {
            resume.UpdateCore(request.ProfessionalSummary);
        }

        var workExperiences = request.WorkExperiences ?? Array.Empty<ResumeWorkExperienceItem>();
        var educations = request.Educations ?? Array.Empty<ResumeEducationItem>();
        var skills = request.Skills ?? Array.Empty<ResumeSkillItem>();
        var languages = request.Languages ?? Array.Empty<ResumeLanguageItem>();
        var projects = request.Projects ?? Array.Empty<ResumeProjectItem>();
        var certifications = request.Certifications ?? Array.Empty<ResumeCertificationItem>();
        var portfolioLinks = request.PortfolioLinks ?? Array.Empty<ResumePortfolioLinkItem>();

        SyncWorkExperiences(resume.Id, resume.WorkExperiences, workExperiences);
        SyncEducations(resume.Id, resume.Educations, educations);
        SyncSkills(resume.Id, resume.Skills, skills, skillRefs);
        SyncLanguages(resume.Id, resume.Languages, languages);
        SyncProjects(resume.Id, resume.Projects, projects);
        SyncCertifications(resume.Id, resume.Certifications, certifications);
        SyncPortfolioLinks(resume.Id, resume.PortfolioLinks, portfolioLinks);

        await dbContext.SaveChangesAsync(cancellationToken);
        var fresh = await LoadByIdAsync(resume.Id, cancellationToken);
        if (fresh is null) throw new InvalidOperationException("Resume was not persisted.");
        return MapToResult(fresh);
    }
    public async Task<ResumeResult> UploadCvAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        var validation = await cvUploadValidator.ValidateAsync(
            new CvUploadRequest(fileName, contentType, fileSize), cancellationToken);
        if (!validation.IsValid)
            throw new DomainValidationException(ToErrorDictionary(validation.Errors));

        var options = fileStorageOptions.Value;
        var storageRoot = Path.Combine(Directory.GetCurrentDirectory(), options.CvStoragePath);
        Directory.CreateDirectory(storageRoot);

        var extension = Path.GetExtension(fileName) ?? string.Empty;
        var storedName = $"cv_{Guid.NewGuid():N}{extension}";
        var storedPath = Path.Combine(storageRoot, storedName);

        await using (var fs = new FileStream(storedPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true))
        {
            await fileStream.CopyToAsync(fs, 65536, cancellationToken);
        }

        var resume = await LoadAsync(userId, cancellationToken);
        if (resume is null)
        {
            resume = CandidateResume.Create(userId);
            dbContext.CandidateResumes.Add(resume);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        resume.SetCvFile(storedName, contentType, fileSize);
        await dbContext.SaveChangesAsync(cancellationToken);

        var fresh = await LoadByIdAsync(resume.Id, cancellationToken);
        return MapToResult(fresh!);
    }

    public async Task<ResumeResult> DeleteCvAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var resume = await LoadAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Candidate resume was not found.");

        if (string.IsNullOrEmpty(resume.CvFileName))
            throw new NotFoundException("CV file was not found.");

        DeleteOnDiskIfExists(resume.CvFileName);
        resume.ClearCvFile();
        await dbContext.SaveChangesAsync(cancellationToken);

        var fresh = await LoadByIdAsync(resume.Id, cancellationToken);
        return MapToResult(fresh!);
    }
    public async Task<CvDownload> DownloadCvAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var resume = await LoadAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Candidate resume was not found.");

        if (string.IsNullOrEmpty(resume.CvFileName) || string.IsNullOrEmpty(resume.CvFileContentType))
            throw new NotFoundException("CV file was not found.");

        var options = fileStorageOptions.Value;
        var storedPath = Path.Combine(Directory.GetCurrentDirectory(), options.CvStoragePath, resume.CvFileName);
        if (!File.Exists(storedPath))
            throw new NotFoundException("CV file was not found.");

        var content = new MemoryStream();
        await using (var fs = new FileStream(storedPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
        {
            await fs.CopyToAsync(content, 4096, cancellationToken);
        }
        content.Position = 0;
        return new CvDownload(content, resume.CvFileContentType, BuildDownloadName(resume.CvFileContentType));
    }

    public async Task<ResumeResult> SetStatusAsync(Guid userId, string status, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<ResumeStatus>(status, ignoreCase: true, out var resumeStatus))
            throw new DomainValidationException("وضعیت رزومه نامعتبر است.");

        var resume = await LoadAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Candidate resume was not found.");

        resume.SetStatus(resumeStatus);
        await dbContext.SaveChangesAsync(cancellationToken);

        var fresh = await LoadByIdAsync(resume.Id, cancellationToken);
        return MapToResult(fresh!);
    }

    public async Task DeleteResumeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var resume = await LoadAsync(userId, cancellationToken);
        if (resume is null) return;

        if (!string.IsNullOrEmpty(resume.CvFileName))
            DeleteOnDiskIfExists(resume.CvFileName);

        dbContext.CandidateResumes.Remove(resume);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateAsync(SaveResumeRequest request, CancellationToken cancellationToken)
    {
        var validation = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new DomainValidationException(ToErrorDictionary(validation.Errors));
    }

    private Task<CandidateResume?> LoadAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.CandidateResumes
            .Include(r => r.WorkExperiences)
            .Include(r => r.Educations)
            .Include(r => r.Skills)
            .Include(r => r.Languages)
            .Include(r => r.Projects)
            .Include(r => r.Certifications)
            .Include(r => r.PortfolioLinks)
            .SingleOrDefaultAsync(r => r.UserId == userId, cancellationToken);

    private Task<CandidateResume?> LoadByIdAsync(Guid resumeId, CancellationToken cancellationToken) =>
        dbContext.CandidateResumes
            .AsNoTracking()
            .Include(r => r.WorkExperiences)
            .Include(r => r.Educations)
            .Include(r => r.Skills)
            .Include(r => r.Languages)
            .Include(r => r.Projects)
            .Include(r => r.Certifications)
            .Include(r => r.PortfolioLinks)
            .SingleOrDefaultAsync(r => r.Id == resumeId, cancellationToken);

    private void DeleteOnDiskIfExists(string fileName)
    {
        var options = fileStorageOptions.Value;
        var storedPath = Path.Combine(Directory.GetCurrentDirectory(), options.CvStoragePath, fileName);
        if (File.Exists(storedPath)) File.Delete(storedPath);
    }

    private static string BuildDownloadName(string contentType) =>
        contentType?.ToLowerInvariant() switch
        {
            "application/pdf" => "cv.pdf",
            "application/msword" => "cv.doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "cv.docx",
            _ => "cv.bin"
        };
    private void SyncWorkExperiences(Guid resumeId, IReadOnlyCollection<ResumeWorkExperience> existing, ResumeWorkExperienceItem[] items)
    {
        var existingList = existing.ToList();
        var requestedIds = items.Where(i => i.Id.HasValue && i.Id.Value != Guid.Empty).Select(i => i.Id!.Value).ToHashSet();

        foreach (var entry in existingList.Where(e => !requestedIds.Contains(e.Id)).ToList())
            dbContext.ResumeWorkExperiences.Remove(entry);

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item.Id.HasValue && item.Id.Value != Guid.Empty)
            {
                var existingEntity = existingList.FirstOrDefault(e => e.Id == item.Id.Value);
                existingEntity?.Update(item.Company, item.JobTitle, item.Description, item.StartDate, item.EndDate, item.IsCurrent);
            }
            else
            {
                var entity = ResumeWorkExperience.Create(resumeId, item.Company, item.JobTitle, item.Description, item.StartDate, item.EndDate, item.IsCurrent, i);
                dbContext.ResumeWorkExperiences.Add(entity);
            }
        }
    }

    private void SyncEducations(Guid resumeId, IReadOnlyCollection<ResumeEducation> existing, ResumeEducationItem[] items)
    {
        var existingList = existing.ToList();
        var requestedIds = items.Where(i => i.Id.HasValue && i.Id.Value != Guid.Empty).Select(i => i.Id!.Value).ToHashSet();

        foreach (var entry in existingList.Where(e => !requestedIds.Contains(e.Id)).ToList())
            dbContext.ResumeEducations.Remove(entry);

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item.Id.HasValue && item.Id.Value != Guid.Empty)
            {
                var existingEntity = existingList.FirstOrDefault(e => e.Id == item.Id.Value);
                existingEntity?.Update(item.Institution, item.Degree, item.FieldOfStudy, item.Grade, item.StartDate, item.EndDate, item.IsCurrent);
            }
            else
            {
                var entity = ResumeEducation.Create(resumeId, item.Institution, item.Degree, item.FieldOfStudy, item.Grade, item.StartDate, item.EndDate, item.IsCurrent, i);
                dbContext.ResumeEducations.Add(entity);
            }
        }
    }

    private void SyncSkills(Guid resumeId, IReadOnlyCollection<ResumeSkill> existing, ResumeSkillItem[] items, IDictionary<Guid, string> skillReferences)
    {
        var existingList = existing.ToList();
        var requestedIds = items.Where(i => i.Id.HasValue && i.Id.Value != Guid.Empty).Select(i => i.Id!.Value).ToHashSet();

        foreach (var entry in existingList.Where(e => !requestedIds.Contains(e.Id)).ToList())
            dbContext.ResumeSkills.Remove(entry);

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item.Id.HasValue && item.Id.Value != Guid.Empty)
            {
                var existingEntity = existingList.FirstOrDefault(e => e.Id == item.Id.Value);
                existingEntity?.Update(item.ProficiencyLevel, item.YearsOfExperience);
            }
            else
            {
                var skillName = skillReferences.TryGetValue(item.SkillId, out var name) ? name : item.SkillName;
                var entity = ResumeSkill.Create(resumeId, item.SkillId, skillName, item.ProficiencyLevel, item.YearsOfExperience, i);
                dbContext.ResumeSkills.Add(entity);
            }
        }
    }

    private void SyncLanguages(Guid resumeId, IReadOnlyCollection<ResumeLanguage> existing, ResumeLanguageItem[] items)
    {
        var existingList = existing.ToList();
        var requestedIds = items.Where(i => i.Id.HasValue && i.Id.Value != Guid.Empty).Select(i => i.Id!.Value).ToHashSet();

        foreach (var entry in existingList.Where(e => !requestedIds.Contains(e.Id)).ToList())
            dbContext.ResumeLanguages.Remove(entry);

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item.Id.HasValue && item.Id.Value != Guid.Empty)
            {
                var existingEntity = existingList.FirstOrDefault(e => e.Id == item.Id.Value);
                existingEntity?.Update(item.LanguageName, item.ProficiencyLevel);
            }
            else
            {
                var entity = ResumeLanguage.Create(resumeId, item.LanguageName, item.ProficiencyLevel, i);
                dbContext.ResumeLanguages.Add(entity);
            }
        }
    }
    private void SyncProjects(Guid resumeId, IReadOnlyCollection<ResumeProject> existing, ResumeProjectItem[] items)
    {
        var existingList = existing.ToList();
        var requestedIds = items.Where(i => i.Id.HasValue && i.Id.Value != Guid.Empty).Select(i => i.Id!.Value).ToHashSet();

        foreach (var entry in existingList.Where(e => !requestedIds.Contains(e.Id)).ToList())
            dbContext.ResumeProjects.Remove(entry);

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item.Id.HasValue && item.Id.Value != Guid.Empty)
            {
                var existingEntity = existingList.FirstOrDefault(e => e.Id == item.Id.Value);
                existingEntity?.Update(item.Title, item.Description, item.Url, item.StartDate, item.EndDate, item.IsCurrent);
            }
            else
            {
                var entity = ResumeProject.Create(resumeId, item.Title, item.Description, item.Url, item.StartDate, item.EndDate, item.IsCurrent, i);
                dbContext.ResumeProjects.Add(entity);
            }
        }
    }

    private void SyncCertifications(Guid resumeId, IReadOnlyCollection<ResumeCertification> existing, ResumeCertificationItem[] items)
    {
        var existingList = existing.ToList();
        var requestedIds = items.Where(i => i.Id.HasValue && i.Id.Value != Guid.Empty).Select(i => i.Id!.Value).ToHashSet();

        foreach (var entry in existingList.Where(e => !requestedIds.Contains(e.Id)).ToList())
            dbContext.ResumeCertifications.Remove(entry);

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item.Id.HasValue && item.Id.Value != Guid.Empty)
            {
                var existingEntity = existingList.FirstOrDefault(e => e.Id == item.Id.Value);
                existingEntity?.Update(item.Name, item.IssuingOrganization, item.IssueDate, item.ExpiryDate, item.CertificateUrl);
            }
            else
            {
                var entity = ResumeCertification.Create(resumeId, item.Name, item.IssuingOrganization, item.IssueDate, item.ExpiryDate, item.CertificateUrl, i);
                dbContext.ResumeCertifications.Add(entity);
            }
        }
    }

    private void SyncPortfolioLinks(Guid resumeId, IReadOnlyCollection<ResumePortfolioLink> existing, ResumePortfolioLinkItem[] items)
    {
        var existingList = existing.ToList();
        var requestedIds = items.Where(i => i.Id.HasValue && i.Id.Value != Guid.Empty).Select(i => i.Id!.Value).ToHashSet();

        foreach (var entry in existingList.Where(e => !requestedIds.Contains(e.Id)).ToList())
            dbContext.ResumePortfolioLinks.Remove(entry);

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item.Id.HasValue && item.Id.Value != Guid.Empty)
            {
                var existingEntity = existingList.FirstOrDefault(e => e.Id == item.Id.Value);
                existingEntity?.Update(item.Title, item.Url, item.Description);
            }
            else
            {
                var entity = ResumePortfolioLink.Create(resumeId, item.Title, item.Url, item.Description, i);
                dbContext.ResumePortfolioLinks.Add(entity);
            }
        }
    }
    private ResumeResult MapToResult(CandidateResume resume)
    {
        return new ResumeResult(
            resume.Id,
            resume.UserId,
            resume.ProfessionalSummary,
            resume.Status.ToString(),
            resume.CalculateCompleteness(),
            resume.CvFileName is not null,
            resume.CvFileSizeBytes,
            resume.CvUploadedAt,
            resume.WorkExperiences.OrderBy(e => e.SortOrder).Select(ToWorkExperienceDto).ToArray(),
            resume.Educations.OrderBy(e => e.SortOrder).Select(ToEducationDto).ToArray(),
            resume.Skills.OrderBy(e => e.SortOrder).Select(ToSkillDto).ToArray(),
            resume.Languages.OrderBy(e => e.SortOrder).Select(ToLanguageDto).ToArray(),
            resume.Projects.OrderBy(e => e.SortOrder).Select(ToProjectDto).ToArray(),
            resume.Certifications.OrderBy(e => e.SortOrder).Select(ToCertificationDto).ToArray(),
            resume.PortfolioLinks.OrderBy(e => e.SortOrder).Select(ToPortfolioLinkDto).ToArray(),
            resume.CreatedAt,
            resume.UpdatedAt);
    }

    private static ResumeSectionDto ToWorkExperienceDto(ResumeWorkExperience e) => new(
        e.Id, e.Company, e.JobTitle, e.Description, e.StartDate, e.EndDate, e.IsCurrent,
        DurationMonths(e.StartDate, e.EndDate, e.IsCurrent), e.SortOrder);

    private static ResumeSectionDto ToEducationDto(ResumeEducation e) => new(
        e.Id, e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate, e.IsCurrent,
        DurationMonths(e.StartDate, e.EndDate, e.IsCurrent), e.SortOrder);

    private static ResumeSkillDto ToSkillDto(ResumeSkill s) => new(
        s.Id, s.SkillId, s.SkillName, s.ProficiencyLevel, s.YearsOfExperience, s.SortOrder);

    private static ResumeLanguageDto ToLanguageDto(ResumeLanguage l) => new(
        l.Id, l.LanguageName, l.ProficiencyLevel, l.SortOrder);

    private static ResumeProjectDto ToProjectDto(ResumeProject p) => new(
        p.Id, p.Title, p.Description, p.Url, p.StartDate, p.EndDate, p.IsCurrent, p.SortOrder);

    private static ResumeCertificationDto ToCertificationDto(ResumeCertification c) => new(
        c.Id, c.Name, c.IssuingOrganization, c.IssueDate, c.ExpiryDate, c.CertificateUrl, c.SortOrder);

    private static ResumePortfolioLinkDto ToPortfolioLinkDto(ResumePortfolioLink p) => new(
        p.Id, p.Title, p.Url, p.Description, p.SortOrder);

    private static int DurationMonths(DateOnly? startDate, DateOnly? endDate, bool isCurrent)
    {
        if (startDate is null) return 0;
        var end = (isCurrent || endDate is null) ? DateOnly.FromDateTime(DateTime.UtcNow.Date) : endDate;
        var months = (end.Value.Year - startDate.Value.Year) * 12 + (end.Value.Month - startDate.Value.Month);
        return months < 0 ? 0 : months;
    }

    private static Dictionary<string, string[]> ToErrorDictionary(IList<ValidationFailure> errors) =>
        errors.GroupBy(error => error.PropertyName)
              .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());





}