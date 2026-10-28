using System.Text;
using BaseCleanArchitecture.WebAPI.Middleware.AuditLogging;

namespace BaseCleanArchitecture.Tests.Middleware;

public class AuditBodyCaptureTests
{
    private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void Text_Under_Limit_Is_Stored_Verbatim()
    {
        var body = Bytes("{\"a\":1}");

        var text = AuditBodyCapture.ToStoredText(body, body.Length, "application/json", 1000, out var truncated);

        Assert.Equal("{\"a\":1}", text);
        Assert.False(truncated);
    }

    [Fact]
    public void Text_Over_Limit_Is_Cut_At_MaxBytes_And_Flagged()
    {
        var captured = Bytes(new string('a', 10));

        var text = AuditBodyCapture.ToStoredText(captured, 5000, "text/plain", 10, out var truncated);

        Assert.Equal(new string('a', 10), text);
        Assert.True(truncated);
    }

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("multipart/form-data; boundary=x")]
    [InlineData("image/png")]
    public void Non_Text_Body_Is_Replaced_By_Placeholder(string contentType)
    {
        var text = AuditBodyCapture.ToStoredText(new byte[] { 1, 2, 3 }, 123, contentType, 1000, out var truncated);

        Assert.Equal("[binary content, 123 bytes]", text);
        Assert.False(truncated);
    }

    [Fact]
    public void Empty_Body_Is_Null()
    {
        var text = AuditBodyCapture.ToStoredText(ReadOnlySpan<byte>.Empty, 0, "application/json", 1000, out var truncated);

        Assert.Null(text);
        Assert.False(truncated);
    }

    [Theory]
    [InlineData("application/json; charset=utf-8")]
    [InlineData("application/problem+json")]
    [InlineData("application/xml")]
    [InlineData("text/xml")]
    [InlineData("text/html")]
    [InlineData("application/x-www-form-urlencoded")]
    [InlineData(null)]
    public void IsTextContentType_Accepts_Text_Like_Types(string? contentType)
    {
        Assert.True(AuditBodyCapture.IsTextContentType(contentType));
    }

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("multipart/form-data")]
    [InlineData("application/pdf")]
    public void IsTextContentType_Rejects_Binary_Types(string contentType)
    {
        Assert.False(AuditBodyCapture.IsTextContentType(contentType));
    }
}
