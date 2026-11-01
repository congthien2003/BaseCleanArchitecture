using System.Text;

namespace BaseCleanArchitecture.WebAPI.Middleware.AuditLogging;

public static class AuditBodyCapture
{
    /// <summary>
    /// Whether the request carries a body: a declared Content-Length decides it; otherwise chunked encoding means yes,
    /// and without either only methods that normally carry a payload (not GET/HEAD/DELETE) are assumed to have one.
    /// </summary>
    public static bool RequestHasBody(HttpRequest request)
    {
        if (request.ContentLength.HasValue)
        {
            return request.ContentLength.Value > 0;
        }

        if (request.Headers.TransferEncoding.ToString().Contains("chunked", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !(HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsDelete(request.Method));
    }

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
