namespace IranJob.Modules.Candidates.Application.Contracts;

/// <summary>
/// Combined profile + resume completion overview returned to a candidate
/// so the candidate dashboard can show readiness for applying.
/// </summary>
public sealed record CandidateProfileCompletionResponse(
    int ProfileCompletenessScore,
    bool HasProfile,
    string[] MissingProfileFields,
    ResumeCompletionResponse? Resume,
    ApplicationReadiness Readiness);

public sealed record ResumeCompletionResponse(
    int CompletenessScore,
    bool HasResume,
    string Status,
    bool IsShared,
    bool HasCvFile,
    string[] MissingSections);

public sealed record ApplicationReadiness(
    bool IsReady,
    bool HasRequiredProfile,
    bool HasRecommendedResume,
    string[] RequiredMissing,
    string[] RecommendedMissing,
    string Message);

/// <summary>
/// Persisted-data-based completeness for the candidate profile.
/// </summary>
public sealed record ProfileCompletionResponse(
    int CompletenessScore,
    bool HasProfile,
    string[] MissingFields);
