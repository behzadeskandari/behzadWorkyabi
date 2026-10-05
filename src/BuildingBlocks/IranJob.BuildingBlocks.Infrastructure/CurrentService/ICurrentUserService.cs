namespace IranJob.BuildingBlocks.Infrastructure.CurrentService;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    bool IsInRole(string role);
}
