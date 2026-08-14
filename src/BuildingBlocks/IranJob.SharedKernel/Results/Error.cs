namespace IranJob.SharedKernel.Results;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string message) => new("Validation.Error", message);

    public static Error NotFound(string message) => new("Resource.NotFound", message);

    public static Error Unexpected(string message) => new("Server.Unexpected", message);
}
