namespace Application.Features.CentralDbSync.Models;

using System.Text.RegularExpressions;
using static Application.Features.CentralDbSync.Models.BootstrapRecoveryConstants;

/// <summary>
/// Shared diagnostic sanitizer for operational evidence in both the Application
/// and Infrastructure layers. Redacts secrets BEFORE truncation.
/// </summary>
public static class BootstrapDiagnosticSanitizer
{
    /// Matches known secret-bearing patterns and replaces the secret portion.
    private static readonly Regex SecretPattern = new(
        @"(?i)(?<key>password|pwd|token|secret|apikey|api_key|authorization|connection\s*string)\s*[=:]\s*(?<prefix>bearer\s+)?[^\s,;}]+|(?<jwt>\beyJ[a-zA-Z0-9_-]{20,}\.[a-zA-Z0-9_-]{20,}\.[a-zA-Z0-9_-]{20,}\b)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Applies secret redaction and then truncates to <see cref="MaxDiagnosticLength"/>.
    /// Returns <c>null</c> when the input is null or whitespace.
    /// </summary>
    public static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var sanitized = SecretPattern.Replace(value, m =>
        {
            if (m.Groups["jwt"].Success)
                return "[REDACTED_JWT]";
            return $"{m.Groups["key"].Value}=[REDACTED]";
        });
        return sanitized.Length <= MaxDiagnosticLength ? sanitized : sanitized[..MaxDiagnosticLength];
    }
}
