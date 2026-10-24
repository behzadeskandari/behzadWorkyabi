namespace IranJob.Api.Models;

/// <summary>
/// Standard API error response using ASP.NET Core ProblemDetails conventions.
/// </summary>
public sealed record ErrorResponse(string Code, string Message, string? Details = null);

/// <summary>
/// Paginated list response.
/// </summary>
public sealed record PaginationResponse<T>(IEnumerable<T> Items, int Page, int PageSize, int TotalCount);
