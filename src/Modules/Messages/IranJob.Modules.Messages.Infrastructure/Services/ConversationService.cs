using FluentValidation;
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.Modules.EmployerProfile.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Domain.Enums;
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
using IranJob.Modules.JobPostings.Domain.Entities;

namespace IranJob.Modules.Messages.Infrastructure.Services;

public sealed class ConversationService(
    MessagesDbContext dbContext,
    JobPostingDbContext jobDbContext,
    CandidateDbContext candidateDbContext,
    EmployerDbContext employerDbContext,
    IEmailService emailService,
    INotificationService notificationService,
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

        var eligibility = await ResolveEligibilityAsync(
            userId,
            request.JobPostingId,
            request.JobApplicationId,
            cancellationToken);

        // The module intentionally permits more than one thread per application.
        // Keep that policy: each explicit start request is a new conversation.

        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? eligibility.JobTitle
            : request.Subject.Trim();

        if (string.IsNullOrWhiteSpace(subject))
        {
            subject = "Job conversation";
        }

        var conversation = Conversation.Create(
            subject,
            request.JobPostingId,
            request.JobApplicationId);

        var candidateParticipant = ConversationParticipant.Create(
            conversation.Id,
            eligibility.CandidateProfileId,
            eligibility.CandidateUserId,
            ParticipantRole.Candidate);

        var employerParticipant = ConversationParticipant.Create(
            conversation.Id,
            eligibility.EmployerProfileId,
            eligibility.EmployerUserId,
            ParticipantRole.Employer);

        conversation.AddParticipant(candidateParticipant);
        conversation.AddParticipant(employerParticipant);

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Conversation {ConversationId} created by {UserId} for job {JobPostingId}.",
            conversation.Id,
            userId,
            request.JobPostingId);

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

        var participant = await GetParticipantAsync(
            conversationId,
            userId,
            asNoTracking: true,
            cancellationToken);

        if (participant is null)
        {
            throw new NotFoundException("Conversation was not found.");
        }

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
                    || (readUpTo.HasValue && message.SentAt <= readUpTo.Value),
                message.SenderUserId == userId))
            .ToListAsync(cancellationToken);
    }


    public async Task<UnreadSummary> GetUnreadSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        RequireAuthenticated(userId);

        var items = await (
            from participant in dbContext.ConversationParticipants.AsNoTracking()
            where participant.IdentityUserId == userId
            join message in dbContext.Messages.AsNoTracking()
                on participant.ConversationId equals message.ConversationId
            where message.SenderUserId != userId
                  && (!participant.LastReadMessageAt.HasValue
                      || message.SentAt > participant.LastReadMessageAt.Value)
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

    public async Task MarkAsReadAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        RequireAuthenticated(userId);

        var participant = await GetParticipantAsync(
            conversationId,
            userId,
            asNoTracking: false,
            cancellationToken);

        if (participant is null)
        {
            throw new NotFoundException("Conversation was not found.");
        }

        var newestReceived = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId
                              && message.SenderUserId != userId)
            .OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .Select(message => (DateTimeOffset?)message.SentAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Idempotent: no received messages, or already read up to the newest one.
        if (newestReceived is null
            || (participant.LastReadMessageAt.HasValue
                && participant.LastReadMessageAt.Value >= newestReceived.Value))
        {
            return;
        }

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

        var participant = await GetParticipantAsync(
            request.ConversationId,
            userId,
            asNoTracking: false,
            cancellationToken);

        if (participant is null)
        {
            throw new NotFoundException("Conversation was not found.");
        }

        var conversation = await dbContext.Conversations
            .Where(conversation => conversation.Id == request.ConversationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            throw new NotFoundException("Conversation was not found.");
        }

        var senderName = await ResolveSenderNameAsync(participant, cancellationToken);

        var message = Message.Create(
            request.ConversationId,
            participant.ProfileId,
            userId,
            senderName,
            request.Content.Trim());

        dbContext.Messages.Add(message);

        conversation.RecordActivity(message.SentAt);
        participant.MarkAsRead(message.SentAt);

        await dbContext.SaveChangesAsync(cancellationToken);

        // Notify only the other persisted participant after the message commits.
        // Notification delivery is non-critical and must not undo a sent message.
        try
        {
            var recipient = await dbContext.ConversationParticipants
                .AsNoTracking()
                .Where(item => item.ConversationId == conversation.Id && item.IdentityUserId != userId)
                .Select(item => item.IdentityUserId)
                .SingleOrDefaultAsync(cancellationToken);

            if (recipient != Guid.Empty)
            {
                await notificationService.CreateIfNotExistsAsync(
                    recipient,
                    IranJob.Modules.Notifications.Domain.Enums.NotificationType.MessageReceived,
                    "پیام جدید در گفت‌وگو",
                    "یک پیام جدید برای شما ارسال شده است.",
                    message.Id,
                    cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Failed to create message notification for conversation {ConversationId}.", conversation.Id);
        }

        // Email side effect only after commit; failures are logged and swallowed
        // so the committed message is never rolled back.
        await SendEmailMirrorAsync(conversation.Id, participant, message, cancellationToken);

        return new MessageDto(
            message.Id,
            message.ConversationId,
            message.SenderProfileId,
            message.SenderName,
            message.Content,
            message.SentAt,
            isRead: true,
            isMine: true);
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
        {
            return Array.Empty<ConversationListItem>();
        }

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
            .Select(conversation => new
            {
                conversation.Id,
                conversation.Subject,
                conversation.JobPostingId,
                conversation.LastMessageAt
            })
            .ToListAsync(cancellationToken);

        if (pageOfConversations.Count == 0)
        {
            return Array.Empty<ConversationListItem>();
        }

        var pageIds = pageOfConversations.Select(row => row.Id).ToArray();

        // N+1 avoidance: one aggregate query each for participant counts, caller
        // read watermarks, last-message rows, and unread counts.
        var participantCounts = await dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(participant => pageIds.Contains(participant.ConversationId))
            .GroupBy(participant => participant.ConversationId)
            .Select(group => new
            {
                ConversationId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                row => row.ConversationId,
                row => row.Count,
                cancellationToken);

        var otherParticipants = await dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(participant => pageIds.Contains(participant.ConversationId)
                                  && participant.IdentityUserId != userId)
            .Select(participant => new { participant.ConversationId, participant.ProfileId, participant.Role })
            .ToListAsync(cancellationToken);
        var candidateIds = otherParticipants.Where(p => p.Role == ParticipantRole.Candidate).Select(p => p.ProfileId).Distinct().ToArray();
        var employerIds = otherParticipants.Where(p => p.Role == ParticipantRole.Employer).Select(p => p.ProfileId).Distinct().ToArray();
        var candidateNames = candidateIds.Length == 0 ? new Dictionary<Guid, string>() : await candidateDbContext.CandidateProfiles
            .AsNoTracking().Where(profile => candidateIds.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id, profile => profile.Headline ?? "Candidate", cancellationToken);
        var employerNames = employerIds.Length == 0 ? new Dictionary<Guid, string>() : await employerDbContext.EmployerProfiles
            .AsNoTracking().Where(profile => employerIds.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id, profile => profile.CompanyName ?? "Employer", cancellationToken);
        var otherNames = otherParticipants.ToDictionary(
            participant => participant.ConversationId,
            participant => participant.Role == ParticipantRole.Candidate
                ? candidateNames.GetValueOrDefault(participant.ProfileId) ?? "Candidate"
                : employerNames.GetValueOrDefault(participant.ProfileId) ?? "Employer");

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
                select new
                {
                    ConversationId = grouped.Key,
                    MaxSentAt = grouped.Max(message => message.SentAt)
                })
                on new { message.ConversationId, message.SentAt }
                    equals new { latest.ConversationId, SentAt = latest.MaxSentAt }
            orderby message.SentAt, message.Id
            select new { message.ConversationId, message.Content })
            .ToListAsync(cancellationToken);

        var previewByConversation = lastMessageRows
            .GroupBy(row => row.ConversationId)
            .ToDictionary(
                group => group.Key,
                group => TruncatePreview(group.Last().Content));

        var unreadCounts = await (
            from participant in dbContext.ConversationParticipants.AsNoTracking()
            where pageIds.Contains(participant.ConversationId)
                  && participant.IdentityUserId == userId
            join message in dbContext.Messages.AsNoTracking()
                on participant.ConversationId equals message.ConversationId
            where message.SenderUserId != userId
                  && (!participant.LastReadMessageAt.HasValue
                      || message.SentAt > participant.LastReadMessageAt.Value)
            group message by participant.ConversationId into grouped
            select new
            {
                ConversationId = grouped.Key,
                Count = grouped.Count()
            })
            .ToDictionaryAsync(
                row => row.ConversationId,
                row => row.Count,
                cancellationToken);

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
            JobTitle = row.JobPostingId.HasValue && jobTitles.TryGetValue(
                    row.JobPostingId.Value, out var title)
                ? title
                : null,
            LastMessagePreview = previewByConversation.TryGetValue(
                    row.Id, out var preview)
                ? preview
                : null,
            LastMessageAt = lastMessages.TryGetValue(row.Id, out var last)
                ? last.MaxSentAt
                : row.LastMessageAt,
            UnreadCount = unreadCounts.TryGetValue(row.Id, out var unread)
                ? unread
                : 0,
            ParticipantCount = participantCounts.TryGetValue(row.Id, out var count)
                ? count
                : 0,
            OtherParticipantName = otherNames.GetValueOrDefault(row.Id)
        }).ToArray();
    }


    private async Task<EligibilityResult> ResolveEligibilityAsync(
        Guid userId,
        Guid jobPostingId,
        Guid? jobApplicationId,
        CancellationToken cancellationToken)
    {
        var jobPosting = await jobDbContext.JobPostings
            .FirstOrDefaultAsync(job => job.Id == jobPostingId, cancellationToken);

        if (jobPosting is null)
        {
            throw new NotFoundException("Job posting not found.");
        }

        if (jobPosting.Status != JobPostingStatus.Published)
        {
            throw new DomainException("Job posting is not published and cannot be messaged about.");
        }

        var application = await jobDbContext.JobApplications
            .FirstOrDefaultAsync(application => application.Id == jobApplicationId
                && application.JobPostingId == jobPostingId
                && (application.Status == JobApplicationStatus.Applied
                    || application.Status == JobApplicationStatus.Reviewed
                    || application.Status == JobApplicationStatus.Accepted), cancellationToken);
        if (application is null)
        {
            throw new NotFoundException("Eligible job application not found.");
        }

        var candidateProfile = await candidateDbContext.CandidateProfiles
            .FirstOrDefaultAsync(profile => profile.Id == application.CandidateProfileId, cancellationToken);
        if (candidateProfile is null)
        {
            throw new DomainException("The application candidate profile is unavailable.");
        }

        // Employer path: the authenticated user owns the job posting.
        var employerProfile = await employerDbContext.EmployerProfiles
            .FirstOrDefaultAsync(
                profile => profile.Id == jobPosting.EmployerProfileId
                           && profile.UserId == userId,
                cancellationToken);

        if (employerProfile is not null)
        {
            return new EligibilityResult(
                candidateProfile.Id,
                candidateProfile.UserId,
                employerProfile.Id,
                employerProfile.UserId,
                jobPosting.Title);
        }

        // Candidate path: the authenticated user must have an active application
        // for this job posting.
        if (candidateProfile.UserId != userId)
        {
            throw new DomainException("User has not applied to this job posting.");
        }

        return new EligibilityResult(
            candidateProfile.Id,
            candidateProfile.UserId,
            default,
            default,
            jobPosting.Title);
    }

    private async Task<ConversationParticipant?> GetParticipantAsync(
        Guid conversationId,
        Guid userId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ConversationParticipants
            .Where(participant => participant.ConversationId == conversationId
                                  && participant.IdentityUserId == userId);

        return asNoTracking
            ? await query.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
            : await query.FirstOrDefaultAsync(cancellationToken);
    }


    private async Task<ConversationListItem?> ProjectListItemAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(conversation => conversation.Id == conversationId, cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        var participant = await dbContext.ConversationParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(
                participant => participant.ConversationId == conversationId
                               && participant.IdentityUserId == userId,
                cancellationToken);

        if (participant is null)
        {
            throw new NotFoundException("Conversation was not found.");
        }

        var lastMessageAt = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .Select(message => (DateTimeOffset?)message.SentAt)
            .FirstOrDefaultAsync(cancellationToken);

        var lastMessageTime = lastMessageAt ?? conversation.LastMessageAt;

        var jobTitle = conversation.JobPostingId is null
            ? null
            : (await jobDbContext.JobPostings
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    job => job.Id == conversation.JobPostingId.Value,
                    cancellationToken))?.Title;

        var unreadCount = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId
                              && message.SenderUserId != userId
                              && (!participant.LastReadMessageAt.HasValue
                                  || message.SentAt > participant.LastReadMessageAt.Value))
            .CountAsync(cancellationToken);

        var otherParticipant = await dbContext.ConversationParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(participant => participant.ConversationId == conversationId
                                                && participant.IdentityUserId != userId,
                cancellationToken);
        var otherName = otherParticipant is null
            ? null
            : await ResolveSenderNameAsync(otherParticipant, cancellationToken);
        var participantCount = await dbContext.ConversationParticipants
            .AsNoTracking()
            .CountAsync(participant => participant.ConversationId == conversationId, cancellationToken);

        return new ConversationListItem
        {
            Id = conversation.Id,
            Subject = conversation.Subject,
            JobPostingId = conversation.JobPostingId,
            JobTitle = jobTitle,
            LastMessagePreview = TruncatePreview(await GetLastMessageContentAsync(conversationId, cancellationToken) ?? string.Empty),
            LastMessageAt = lastMessageTime,
            UnreadCount = unreadCount,
            ParticipantCount = participantCount,
            OtherParticipantName = otherName
        };
    }

    private async Task<string?> GetLastMessageContentAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .Select(message => message.Content)
            .FirstOrDefaultAsync(cancellationToken);
    }


    private async Task<string> ResolveSenderNameAsync(
        ConversationParticipant participant,
        CancellationToken cancellationToken)
    {
        if (participant.Role == ParticipantRole.Candidate)
        {
            var candidate = await candidateDbContext.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    profile => profile.Id == participant.ProfileId,
                    cancellationToken);

            return string.IsNullOrWhiteSpace(candidate?.Headline)
                ? "Candidate"
                : candidate.Headline.Trim();
        }

        var employer = await employerDbContext.EmployerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                profile => profile.Id == participant.ProfileId,
                cancellationToken);

        return string.IsNullOrWhiteSpace(employer?.CompanyName)
            ? "Employer"
            : employer.CompanyName.Trim();
    }

    private async Task SendEmailMirrorAsync(
        Guid conversationId,
        ConversationParticipant sender,
        Message message,
        CancellationToken cancellationToken)
    {
        // The recipient of a two-party conversation is the other participant.
        var recipientParticipant = await dbContext.ConversationParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(participant => participant.ConversationId == conversationId
                                           && participant.IdentityUserId != sender.IdentityUserId,
                cancellationToken);

        if (recipientParticipant is null)
        {
            logger.LogDebug(
                "No recipient participant found for conversation {ConversationId}; skipping email mirror.",
                conversationId);
            return;
        }

        var recipientEmail = await userEmailResolver.ResolveEmailAsync(
            recipientParticipant.IdentityUserId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            logger.LogDebug(
                "Recipient {RecipientProfileId} has no email address; skipping email mirror for conversation {ConversationId}.",
                recipientParticipant?.ProfileId,
                conversationId);
            return;
        }

        try
        {
            await emailService.SendAsync(
                new EmailMessage(
                    recipientEmail,
                    "New message in conversation " + conversationId,
                    message.Content),
                cancellationToken);
        }
        catch (Exception exception)
        {
            // Transport failures are non-fatal: the message and read watermark are
            // already committed. Log for observability and swallow so the email path
            // can never roll back the business transaction.
            logger.LogError(
                exception,
                "Failed to send email mirror for message {MessageId} in conversation {ConversationId}.",
                message.Id,
                conversationId);
        }
    }

    private static async Task ValidateAsync<T>(
        IValidator<T> validator,
        T request,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);

        if (!result.IsValid)
        {
            throw new DomainValidationException(
                result.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage).ToArray()));
        }
    }

    private static void RequireAuthenticated(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("An authenticated user is required.");
        }
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new DomainValidationException(
                new Dictionary<string, string[]>
                {
                    ["Page"] = ["Page must be greater than zero."]
                });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new DomainValidationException(
                new Dictionary<string, string[]>
                {
                    ["PageSize"] = ["Page size must be between 1 and 100."]
                });
        }
    }

    private static string TruncatePreview(string content)
    {
        const int maxLength = 120;

        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var trimmed = content.Trim();

        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength] + "...";
    }

    private sealed record EligibilityResult(
        Guid CandidateProfileId,
        Guid CandidateUserId,
        Guid EmployerProfileId,
        Guid EmployerUserId,
        string JobTitle);
}
