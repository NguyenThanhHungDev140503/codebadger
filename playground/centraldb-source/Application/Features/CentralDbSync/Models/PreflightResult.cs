namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Result of a scalable bootstrap preflight validation.
/// <c>IsValid</c> is <c>true</c> when all checks pass.
/// On failure, <c>ErrorCode</c> and <c>ErrorMessage</c> carry safe bounded details.
/// </summary>
public sealed record PreflightResult
{
    public bool IsValid { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static PreflightResult Valid() => new() { IsValid = true };

    public static PreflightResult Fail(string code, string message) => new()
    {
        IsValid = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}
