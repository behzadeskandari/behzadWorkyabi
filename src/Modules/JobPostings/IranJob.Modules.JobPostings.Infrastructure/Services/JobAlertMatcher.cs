using IranJob.Modules.JobPostings.Application.Contracts;
using IranJob.Modules.Candidates.Infrastructure.Persistence;
using IranJob.Modules.JobPostings.Domain.Enums;
using IranJob.Modules.JobPostings.Infrastructure.Persistence;
using IranJob.Modules.Notifications.Application.Abstractions;
using IranJob.Modules.Notifications.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IranJob.Modules.JobPostings.Infrastructure.Services;

public sealed class JobAlertMatcher(
    JobPostingDbContext jobsDb,
    CandidateDbContext candidateDb,
    INotificationService notifications,
    ILogger<JobAlertMatcher> logger) : IJobAlertMatcher
{
    public async Task NotifyMatchesAsync(Guid jobId, CancellationToken ct)
    {
        try
        {
            var job = await jobsDb.JobPostings.AsNoTracking().Include(x => x.Skills)
                .SingleOrDefaultAsync(x => x.Id == jobId && x.Status == JobPostingStatus.Published, ct);
            if (job is null) return;

            var alerts = await jobsDb.JobAlerts.AsNoTracking().Include(x => x.Skills)
                .Where(x => x.IsEnabled
                    && (!x.CategoryId.HasValue || x.CategoryId == job.CategoryId)
                    && (!x.CityId.HasValue || x.CityId == job.CityId)
                    && (x.WorkArrangement == null || x.WorkArrangement == job.WorkArrangement.ToString()))
                .ToListAsync(ct);
            var jobSkillIds = job.Skills.Select(x => x.SkillId).ToHashSet();
            var matchingAlerts = alerts.Where(x => x.Skills.All(skill => jobSkillIds.Contains(skill.SkillId))).ToArray();
            if (matchingAlerts.Length == 0) return;
            var profileIds = matchingAlerts.Select(x => x.CandidateProfileId).Distinct().ToArray();
            var userIds = await candidateDb.CandidateProfiles.AsNoTracking()
                .Where(profile => profileIds.Contains(profile.Id))
                .ToDictionaryAsync(profile => profile.Id, profile => profile.UserId, ct);

            foreach (var alert in matchingAlerts)
            {
                try
                {
                    if (!userIds.TryGetValue(alert.CandidateProfileId, out var userId)) continue;
                    await notifications.CreateIfNotExistsAsync(
                        userId,
                        NotificationType.JobAlertMatch,
                        $"فرصت شغلی جدید: {job.Title}",
                        "این آگهی با یکی از هشدارهای شغلی فعال شما مطابقت دارد.",
                        job.Id,
                        ct);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to notify candidate for alert {AlertId} and job {JobId}.", alert.Id, job.Id);
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Job alert matching failed for published job {JobId}.", jobId);
        }
    }
}
