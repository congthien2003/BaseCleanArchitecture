using System.Text;

namespace BaseCleanArchitecture.WebAPI.Middleware;

public static class AuditBodyCapture
{
    /// <summary>
    /// Turns a captured body into the text to store. <paramref name="captured"/> holds at most the first bytes of the body;
    /// <paramref name="totalBytes"/> is the real size. Returns null for an empty body.
    /// </summary>
    public static string? ToStoredText(ReadOnlySpan<byte> captured, long totalBytes, string? contentType, int maxBytes, out bool truncated)
    {
        truncated = false;

        if (totalBytes <= 0)
        {
            return null;
        }

        if (!IsTextContentType(contentType))
        {
            return $"[binary content, {totalBytes} bytes]";
        }

        truncated = totalBytes > maxBytes;
        return Encoding.UTF8.GetString(captured[..Math.Min(captured.Length, maxBytes)]);
    }

    /// <summary>JSON, XML, text/* and form-urlencoded are text. A missing content type is assumed to be text.</summary>
    public static bool IsTextContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return true;
        }

        var mediaType = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();

        return mediaType.StartsWith("text/", StringComparison.Ordinal)
            || mediaType.EndsWith("json", StringComparison.Ordinal)
            || mediaType.EndsWith("xml", StringComparison.Ordinal)
            || mediaType == "application/x-www-form-urlencoded";
    }
}
