using FluentValidation;
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.Modules.EmployerProfile.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Domain.Enums;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.Messages.Application.Abstractions;
using IranJob.Modules.Messages.Application.Contracts;
using IranJob.Modules.Messages.Domain.Entities;
using IranJob.Modules.Messages.Domain.Enums;
using IranJob.Modules.Messages.Infrastructure.Persistence;
using IranJob.Modules.Notifications.Application.Abstractions;
using IranJob.Modules.Notifications.Application.Contracts;
using IranJob.SharedKernel.Abstractions;
using IranJob.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DomainValidationException = IranJob.SharedKernel.Exceptions.ValidationException;

namespace IranJob.Modules.Messages.Infrastructure.Services;

/// <summary>
/// Candidate/employer direct messaging. Every operation is authorised against the
/// authenticated user id resolved server-side; client-supplied sender or
/// participant ids are never trusted. Ownership and eligibility are resolved from
/// the owning module DbContexts — the same direct cross-DbContext pattern used by
/// JobPostingService and JobApplicationNotificationHandler.
/// </summary>
public sealed class ConversationService(
    MessagesDbContext dbContext,
    JobPostingDbContext jobDbContext,
    CandidateDbContext candidateDbContext,
    EmployerDbContext employerDbContext,
    IEmailService emailService,
    IUserEmailResolver userEmailResolver,
    IValidator<CreateConversationRequest> createValidator,
    IValidator<SendMessageRequest> sendValidator,
    ILogger<ConversationService> logger) : IConversationService
{

 
    public async Task<ConversationListItem?> CreateConversationAsync(
        CreateConversationRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        RequireAuthenticated(userId);
        await ValidateAsync(createValidator, request, cancellationToken);

        var eligibility = await ResolveEligibilityAsync(userId, request.JobPostingId, request.JobApplicationId, cancellationToken);

        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? eligibility.JobTitle
            : request.Subject.Trim();

        var conversation = Conversation.Create(subject, request.JobPostingId, request.JobApplicationId);
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var candidateParticipant = ConversationParticipant.Create(
            conversation.Id, eligibility.CandidateProfileId, eligibility.CandidateUserId, ParticipantRole.Candidate);
        var employerParticipant = ConversationParticipant.Create(
            conversation.Id, eligibility.EmployerProfileId, eligibility.EmployerUserId, ParticipantRole.Employer);
        dbContext.ConversationParticipants.AddRange(candidateParticipant, employerParticipant);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Conversation {Conversation} created for job {Job} by user {User}.",
            conversation.Id, request.JobPostingId, userId);

        return await ProjectListItemAsync(conversation.Id, userId, cancellationToken);
    }

    public async Task<ConversationListItem?> GetConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        RequireAuthenticated(userId);

        return await ProjectListItemAsync(conversationId, userId, cancellationToken);
    }
    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(
        Guid conversationId,
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        RequireAuthenticated(userId);
        ValidatePagination(page, pageSize);

        var participant = await GetParticipantAsync(conversationId, userId, asNoTracking: true, cancellationToken);
        if (participant is null)
            throw new NotFoundException("Conversation was not found.");

        var readUpTo = participant.LastReadMessageAt;

        return await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.SentAt)
            .ThenBy(message => message.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(message => new MessageDto(
                message.Id,
                message.ConversationId,
                message.SenderProfileId,
                message.SenderName,
                message.Content,
                message.SentAt,
                message.SenderUserId == userId
                    || (readUpTo.HasValue && message.SentAt <= readUpTo.Value)))
            .ToListAsync(cancellationToken);
    }

    public async Task<UnreadSummary> GetUnreadSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        RequireAuthenticated(userId);

        var items = await (
            from participant in dbContext.ConversationParticipants.AsNoTracking()
            where participant.IdentityUserId == userId
            join message in dbContext.Messages.AsNoTracking()
                on participant.ConversationId equals message.ConversationId
            where message.SenderUserId != userId
                  && (!participant.LastReadMessageAt.HasValue || message.SentAt > participant.LastReadMessageAt.Value)
            group message by participant.ConversationId into grouped
            select new UnreadItem
            {
                ConversationId = grouped.Key,
                UnreadCount = grouped.Count()
            })
            .ToListAsync(cancellationToken);

        return new UnreadSummary
        {
            TotalUnread = items.Sum(item => item.UnreadCount),
            Items = items
        };
    }

    public async Task MarkAsReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        RequireAuthenticated(userId);

        var participant = await GetParticipantAsync(conversationId, userId, asNoTracking: false, cancellationToken);
        if (participant is null)
            throw new NotFoundException("Conversation was not found.");

        var newestReceived = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId && message.SenderUserId != userId)
            .OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .Select(message => (DateTimeOffset?)message.SentAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Idempotent: no received messages, or already read up to the newest one.
        if (newestReceived is null
            || (participant.LastReadMessageAt.HasValue && participant.LastReadMessageAt.Value >= newestReceived.Value))
            return;

        participant.MarkAsRead(newestReceived.Value);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MessageDto> SendMessageAsync(
        SendMessageRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        RequireAuthenticated(userId);
        await ValidateAsync(sendValidator, request, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Content))
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["Content"] = ["Message content is required."]
            });

        var participant = await GetParticipantAsync(request.ConversationId, userId, asNoTracking: false, cancellationToken);
        if (participant is null)
            throw new NotFoundException("Conversation was not found.");

        var conversation = await dbContext.Conversations
            .Where(c => c.Id == request.ConversationId)
            .FirstOrDefaultAsync(cancellationToken);
        if (conversation is null)
            throw new NotFoundException("Conversation was not found.");

        var senderName = await ResolveSenderNameAsync(participant, cancellationToken);

        var message = Message.Create(request.ConversationId, participant.ProfileId, userId, senderName, request.Content.Trim());
        dbContext.Messages.Add(message);

        conversation.RecordActivity(message.SentAt);
        participant.MarkAsRead(message.SentAt);

        await dbContext.SaveChangesAsync(cancellationToken);

        // Email side effect only after commit; failures are logged and swallowed so
        // the committed message is never rolled back.
        await SendEmailMirrorAsync(conversation.Id, participant, message, cancellationToken);

        return new MessageDto(
            message.Id,
            message.ConversationId,
            message.SenderProfileId,
            message.SenderName,
            message.Content,
            message.SentAt,
            true);
    }

    public async Task<IReadOnlyList<ConversationListItem>> ListConversationsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        RequireAuthenticated(userId);
        ValidatePagination(page, pageSize);

        var conversationIds = await dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(participant => participant.IdentityUserId == userId)
            .Select(participant => participant.ConversationId)
            .ToListAsync(cancellationToken);

        if (conversationIds.Count == 0)
            return Array.Empty<ConversationListItem>();

        // Newest activity first with a stable secondary key, per the module's
        // "latest activity" list contract.
        var pageOfConversations = await dbContext.Conversations
            .AsNoTracking()
            .Where(conversation => conversationIds.Contains(conversation.Id))
            .OrderByDescending(conversation => conversation.LastMessageAt)
            .ThenByDescending(conversation => conversation.CreatedAt)
            .ThenBy(conversation => conversation.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(conversation => new { conversation.Id, conversation.Subject, conversation.JobPostingId, conversation.LastMessageAt })
            .ToListAsync(cancellationToken);

        if (pageOfConversations.Count == 0)
            return Array.Empty<ConversationListItem>();

        var pageIds = pageOfConversations.Select(row => row.Id).ToArray();

        // N+1 avoidance: one aggregate query each for participant counts, caller
        // read watermarks, last-message rows, and unread counts.
        var participantCounts = await dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(participant => pageIds.Contains(participant.ConversationId))
            .GroupBy(participant => participant.ConversationId)
            .Select(group => new { ConversationId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.ConversationId, row => row.Count, cancellationToken);

        var lastMessages = await dbContext.Messages
            .AsNoTracking()
            .Where(message => pageIds.Contains(message.ConversationId))
            .GroupBy(message => message.ConversationId)
            .Select(group => new
            {
                ConversationId = group.Key,
                MaxSentAt = group.Max(message => message.SentAt)
            })
            .ToDictionaryAsync(row => row.ConversationId, cancellationToken);

        var lastMessageRows = await (
            from message in dbContext.Messages.AsNoTracking()
            where pageIds.Contains(message.ConversationId)
            join latest in (
                from inner in dbContext.Messages.AsNoTracking()
                where pageIds.Contains(inner.ConversationId)
                group inner by inner.ConversationId into grouped
                select new { ConversationId = grouped.Key, MaxSentAt = grouped.Max(m => m.SentAt) })
                on new { message.ConversationId, message.SentAt } equals new { latest.ConversationId, SentAt = latest.MaxSentAt }
            orderby message.SentAt, message.Id
            select new { message.ConversationId, message.Content })
            .ToListAsync(cancellationToken);

        var previewByConversation = lastMessageRows
            .GroupBy(row => row.ConversationId)
            .ToDictionary(group => group.Key, group => TruncatePreview(group.Last().Content));

        var unreadCounts = await (
            from participant in dbContext.ConversationParticipants.AsNoTracking()
            where pageIds.Contains(participant.ConversationId) && participant.IdentityUserId == userId
            join message in dbContext.Messages.AsNoTracking()
                on participant.ConversationId equals message.ConversationId
            where message.SenderUserId != userId
                  && (!participant.LastReadMessageAt.HasValue || message.SentAt > participant.LastReadMessageAt.Value)
            group message by participant.ConversationId into grouped
            select new { ConversationId = grouped.Key, Count = grouped.Count() })
            .ToDictionaryAsync(row => row.ConversationId, row => row.Count, cancellationToken);

        var jobIds = pageOfConversations
            .Where(row => row.JobPostingId.HasValue)
            .Select(row => row.JobPostingId!.Value)
            .Distinct()
            .ToArray();

        var jobTitles = jobIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await jobDbContext.JobPostings
                .AsNoTracking()
                .Where(job => jobIds.Contains(job.Id))
                .ToDictionaryAsync(job => job.Id, job => job.Title, cancellationToken);

        return pageOfConversations.Select(row => new ConversationListItem
        {
            Id = row.Id,
            Subject = row.Subject,
            JobPostingId = row.JobPostingId,
            JobTitle = row.JobPostingId.HasValue && jobTitles.TryGetValue(row.JobPostingId.Value, out var title)
                ? title
                : null,
            LastMessagePreview = previewByConversation.TryGetValue(row.Id, out var preview) ? preview : null,
            LastMessageAt = lastMessages.TryGetValue(row.Id, out var last) ? last.MaxSentAt : row.LastMessageAt,
            UnreadCount = unreadCounts.TryGetValue(row.Id, out var unread) ? unread : 0,
            ParticipantCount = participantCounts.TryGetValue(row.Id, out var count) ? count : 0
        }).ToArray();
    }ate async Task<ConversationListItem?> ProjectListItemAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => new { c.Id, c.Subject, c.JobPostingId })
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
            return null;

        var participant = await GetParticipantAsync(conversationId, userId, asNoTracking: true, cancellationToken);
        if (participant is null)
            return null;

        var unreadCount = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId
                              && message.SenderUserId != userId
                              && (!participant.LastReadMessageAt.HasValue || message.SentAt > participant.LastReadMessageAt.Value))
            .CountAsync(cancellationToken);

        var participantCount = await dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(p => p.ConversationId == conversationId)
            .CountAsync(cancellationToken);

        var lastMessage = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .Select(message => new { message.Content, message.SentAt })
            .FirstOrDefaultAsync(cancellationToken);

        string? jobTitle = null;
        if (conversation.JobPostingId.HasValue)
        {
            jobTitle = await jobDbContext.JobPostings
                .AsNoTracking()
                .Where(job => job.Id == conversation.JobPostingId.Value)
                .Select(job => job.Title)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ConversationListItem
        {
            Id = conversation.Id,
            Subject = conversation.Subject,
JobPostingId = conversation.JobPostingId,
            JobTitle = jobTitle,
            LastMessagePreview = lastMessage is null ? null : TruncatePreview(lastMessage.Content),
            LastMessageAt = lastMessage?.SentAt,
            UnreadCount = unreadCount,
            ParticipantCount = participantCount
        };
    }rivate async Task<EligibilityResult> ResolveEligibilityAsync(
        Guid userId,
        Guid jobPostingId,
        Guid? jobApplicationId,
        CancellationToken cancellationToken)
    {
        var job = await jobDbContext.JobPostings
            .AsNoTracking()
            .Where(posting => posting.Id == jobPostingId && posting.Status == JobPostingStatus.Published)
            .Select(posting => new { posting.Id, posting.Title, posting.EmployerProfileId })
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
            throw new NotFoundException("The referenced published job posting was not found.");

        var employerProfile = await employerDbContext.EmployerProfiles
            .AsNoTracking()
            .Where(profile => profile.Id == job.EmployerProfileId)
            .Select(profile => new { profile.Id, profile.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (employerProfile is null)
            throw new NotFoundException("The employer's profile was not found.");

        var candidateProfile = await candidateDbContext.CandidateProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => new { profile.Id, profile.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (userId == employerProfile.UserId)
        {
            if (candidateProfile is not null)
                throw new ForbiddenException("Employers must be the job owner to start this conversation.");

            if (!jobApplicationId.HasValue)
                throw new DomainValidationException(new Dictionary<string, string[]>
                {
                    ["JobApplicationId"] = ["A job application is required to start a conversation as the employer."]
                });

            var applicationProfileId = await jobDbContext.JobApplications
                .AsNoTracking()
                .Where(app => app.Id == jobApplicationId.Value && app.JobPostingId == jobPostingId)
                .Select(app => (Guid?)app.CandidateProfileId)
                .FirstOrDefaultAsync(cancellationToken);

            if (applicationProfileId is null)
                throw new NotFoundException("The referenced job application was not found for this job.");

            var counterparty = await candidateDbContext.CandidateProfiles
                .AsNoTracking()
                .Where(profile => profile.Id == applicationProfileId.Value)
                .Select(profile => new { profile.Id, profile.UserId })
                .FirstOrDefaultAsync(cancellationToken);

            if (counterparty is null)
                throw new NotFoundException("The candidate's profile was not found.");

            return new EligibilityResult(
                job.Title,
                counterparty.Id,
                counterparty.UserId,
                employerProfile.Id,
                employerProfile.UserId,
                jobApplicationId);
        }

        if (candidateProfile is null)
            throw new NotFoundException("A candidate profile is required to start a conversation.");

        if (!jobApplicationId.HasValue)
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["JobApplicationId"] = ["A job application is required to start a conversation as a candidate."]
            });

        var applicationId = await jobDbContext.JobApplications
            .AsNoTracking()
            .Where(app => app.Id == jobApplicationId.Value
    private async Task<string> ResolveSenderNameAsync(ConversationParticipant participant, CancellationToken cancellationToken)
    {
        if (participant.Role == ParticipantRole.Candidate)
        {
            var headline = await candidateDbContext.CandidateProfiles
                .AsNoTracking()
                .Where(profile => profile.Id == participant.ProfileId)
                .Select(profile => profile.Headline)
                .FirstOrDefaultAsync(cancellationToken);

            return string.IsNullOrWhiteSpace(headline) ? "Candidate" : headline.Trim();
        }

        var companyName = await employerDbContext.EmployerProfiles
            .AsNoTracking()
            .Where(profile => profile.Id == participant.ProfileId)
            .Select(profile => profile.CompanyName)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(companyName) ? "Employer" : companyName.Trim();
    }

    private async Task SendEmailMirrorAsync(
        Guid conversationId,
        ConversationParticipant sender,
        Message message,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipientParticipant = await dbContext.ConversationParticipants
                .AsNoTracking()
                .Where(p => p.ConversationId == conversationId && p.IdentityUserId != sender.IdentityUserId)
                .Select(p => p.IdentityUserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (recipientParticipant == Guid.Empty)
                return;

            var recipientEmail = await userEmailResolver.ResolveEmailAsync(recipientParticipant, cancellationToken);
            if (string.IsNullOrWhiteSpace(recipientEmail))
                return;

            await emailService.SendAsync(
                new EmailMessage(
                    recipientEmail,
                    $"New message from {message.SenderName}",
                    message.Content),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to send message email mirror for conversation {Conversation}.", conversationId);
        }
    }

    private async Task<ConversationParticipant?> GetParticipantAsync(
        Guid conversationId,
        Guid userId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ConversationParticipants
            .Where(participant => participant.ConversationId == conversationId && participant.IdentityUserId == userId);

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private static void RequireAuthenticated(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new UnauthorizedException();
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["Page"] = ["Page must be a positive 1-based index."]
            });

        if (pageSize is < 1 or > 100)
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["PageSize"] = ["Page size must be between 1 and 100."]
            });
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new DomainValidationException(result.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
    }

    private static string TruncatePreview(string content)
    {
        const int maxLength = 140;
        if (content.Length <= maxLength)
            return content;

        return content[..maxLength];
    }

    private sealed record EligibilityResult(
        string JobTitle,
        Guid CandidateProfileId,
        Guid CandidateUserId,
        Guid EmployerProfileId,
        Guid EmployerUserId,
        Guid? JobApplicationId);

    private async Task<ConversationListItem?> ProjectListItemAsync(
            job.Title,
            candidateProfile.Id,
            candidateProfile.UserId,
            employerProfile.Id,
            employerProfile.UserId,
            applicationId);
    }

            JobTitle = jobTitle,
            LastMessagePreview = lastMessage is null ? null : TruncatePreview(lastMessage.Content),
            LastMessageAt = lastMessage?.SentAt,
            UnreadCount = unreadCount,
            ParticipantCount = participantCount
        };
    }


    private async Task<ConversationListItem?> ProjectListItemAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => new { c.Id, c.Subject, c.JobPostingId })
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
            return null;

        var participant = await GetParticipantAsync(conversationId, userId, asNoTracking: true, cancellationToken);
        if (participant is null)
            return null;

        var unreadCount = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId
                              && message.SenderUserId != userId
                              && (!participant.LastReadMessageAt.HasValue || message.SentAt > participant.LastReadMessageAt.Value))
            .CountAsync(cancellationToken);

        var participantCount = await dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(p => p.ConversationId == conversationId)
            .CountAsync(cancellationToken);

        var lastMessage = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .Select(message => new { message.Content, message.SentAt })
            .FirstOrDefaultAsync(cancellationToken);

        string? jobTitle = null;
        if (conversation.JobPostingId.HasValue)
        {
            jobTitle = await jobDbContext.JobPostings
                .AsNoTracking()
                .Where(job => job.Id == conversation.JobPostingId.Value)
                .Select(job => job.Title)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ConversationListItem
        {
            Id = conversation.Id,
            Subject = conversation.Subject,
            JobPostingId = conversation.JobPostingId,
            JobTitle = jobTitle,
            LastMessagePreview = lastMessage is null ? null : TruncatePreview(lastMessage.Content),
            LastMessageAt = lastMessage?.SentAt,
            UnreadCount = unreadCount,
            ParticipantCount = participantCount
        };
    }    private async Task<EligibilityResult> ResolveEligibilityAsync(
        Guid userId,
        Guid jobPostingId,
        Guid? jobApplicationId,
        CancellationToken cancellationToken)
    {
        var job = await jobDbContext.JobPostings
            .AsNoTracking()
            .Where(posting => posting.Id == jobPostingId && posting.Status == JobPostingStatus.Published)
            .Select(posting => new { posting.Id, posting.Title, posting.EmployerProfileId })
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
            throw new NotFoundException("The referenced published job posting was not found.");

        var employerProfile = await employerDbContext.EmployerProfiles
            .AsNoTracking()
            .Where(profile => profile.Id == job.EmployerProfileId)
            .Select(profile => new { profile.Id, profile.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (employerProfile is null)
            throw new NotFoundException("The employer's profile was not found.");

        var candidateProfile = await candidateDbContext.CandidateProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => new { profile.Id, profile.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (userId == employerProfile.UserId)
        {
            if (candidateProfile is not null)
                throw new ForbiddenException("Employers must be the job owner to start this conversation.");

            if (!jobApplicationId.HasValue)
                throw new DomainValidationException(new Dictionary<string, string[]>
                {
                    ["JobApplicationId"] = ["A job application is required to start a conversation as the employer."]
                });

            var applicationProfileId = await jobDbContext.JobApplications
                .AsNoTracking()
                .Where(app => app.Id == jobApplicationId.Value && app.JobPostingId == jobPostingId)
                .Select(app => (Guid?)app.CandidateProfileId)
                .FirstOrDefaultAsync(cancellationToken);

            if (applicationProfileId is null)
                throw new NotFoundException("The referenced job application was not found for this job.");

            var counterparty = await candidateDbContext.CandidateProfiles
                .AsNoTracking()
                .Where(profile => profile.Id == applicationProfileId.Value)
                .Select(profile => new { profile.Id, profile.UserId })
                .FirstOrDefaultAsync(cancellationToken);

            if (counterparty is null)
                throw new NotFoundException("The candidate's profile was not found.");

            return new EligibilityResult(
                job.Title,
                counterparty.Id,
                counterparty.UserId,
                employerProfile.Id,
                employerProfile.UserId,
                jobApplicationId);
        }

        if (candidateProfile is null)
            throw new NotFoundException("A candidate profile is required to start a conversation.");

        if (!jobApplicationId.HasValue)
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["JobApplicationId"] = ["A job application is required to start a conversation as a candidate."]
            });

        var applicationId = await jobDbContext.JobApplications
            .AsNoTracking()
            .Where(app => app.Id == jobApplicationId.Value
                          && app.JobPostingId == jobPostingId
                          && app.CandidateProfileId == candidateProfile.Id)
            .Select(app => (Guid?)app.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (applicationId is null)
            throw new NotFoundException("The referenced job application was not found for this candidate and job.");

        return new EligibilityResult(
            job.Title,
            candidateProfile.Id,
            candidateProfile.UserId,
            employerProfile.Id,
            employerProfile.UserId,
            applicationId);
    }
    private async Task<string> ResolveSenderNameAsync(ConversationParticipant participant, CancellationToken cancellationToken)
    {
        if (participant.Role == ParticipantRole.Candidate)
        {
            var headline = await candidateDbContext.CandidateProfiles
                .AsNoTracking()
                .Where(profile => profile.Id == participant.ProfileId)
                .Select(profile => profile.Headline)
                .FirstOrDefaultAsync(cancellationToken);

            return string.IsNullOrWhiteSpace(headline) ? "Candidate" : headline.Trim();
        }

        var companyName = await employerDbContext.EmployerProfiles
            .AsNoTracking()
            .Where(profile => profile.Id == participant.ProfileId)
            .Select(profile => profile.CompanyName)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(companyName) ? "Employer" : companyName.Trim();
    }

    private async Task SendEmailMirrorAsync(
        Guid conversationId,
        ConversationParticipant sender,
        Message message,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipientParticipant = await dbContext.ConversationParticipants
                .AsNoTracking()
                .Where(p => p.ConversationId == conversationId && p.IdentityUserId != sender.IdentityUserId)
                .Select(p => p.IdentityUserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (recipientParticipant == Guid.Empty)
                return;

            var recipientEmail = await userEmailResolver.ResolveEmailAsync(recipientParticipant, cancellationToken);
            if (string.IsNullOrWhiteSpace(recipientEmail))
                return;

            await emailService.SendAsync(
                new EmailMessage(
                    recipientEmail,
                    $"New message from {message.SenderName}",
                    message.Content),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to send message email mirror for conversation {Conversation}.", conversationId);
        }
    }

    private async Task<ConversationParticipant?> GetParticipantAsync(
        Guid conversationId,
        Guid userId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ConversationParticipants
            .Where(participant => participant.ConversationId == conversationId && participant.IdentityUserId == userId);

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private static void RequireAuthenticated(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new UnauthorizedException();
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["Page"] = ["Page must be a positive 1-based index."]
            });

        if (pageSize is < 1 or > 100)
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["PageSize"] = ["Page size must be between 1 and 100."]
            });
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new DomainValidationException(result.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
    }

    private static string TruncatePreview(string content)
    {
        const int maxLength = 140;
        if (content.Length <= maxLength)
            return content;

        return content[..maxLength];
    }


    private sealed record EligibilityResult(
        string JobTitle,
        Guid CandidateProfileId,
        Guid CandidateUserId,
        Guid EmployerProfileId,
        Guid EmployerUserId,
        Guid? JobApplicationId);
}