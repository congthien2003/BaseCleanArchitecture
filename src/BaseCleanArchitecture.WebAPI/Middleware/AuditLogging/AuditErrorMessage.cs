using System.Text.Json;

namespace BaseCleanArchitecture.WebAPI.Middleware.AuditLogging;

public static class AuditErrorMessage
{
    private const int MaxLength = 4000; // AuditLogs.ErrorMessage column size

    private static readonly string[] CandidateProperties = ["message", "detail", "title"];

    public static string FromException(Exception exception) =>
        Cap($"{exception.GetType().Name}: {exception.Message}");

    /// <summary>Reads message, then detail, then title from a JSON error response. Null when there is nothing usable.</summary>
    public static string? FromResponse(string? responseText, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(responseText) || contentType is null || !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseText);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var name in CandidateProperties)
            {
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                        && property.Value.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(property.Value.GetString()))
                    {
                        return Cap(property.Value.GetString()!);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // truncated or not really JSON
        }

        return null;
    }

    // PostgreSQL text columns reject U+0000, and one rejected row must not cost a whole batch.
    private static string Cap(string value)
    {
        value = value.Replace("\0", string.Empty);
        return value.Length <= MaxLength ? value : value[..MaxLength];
    }
}
