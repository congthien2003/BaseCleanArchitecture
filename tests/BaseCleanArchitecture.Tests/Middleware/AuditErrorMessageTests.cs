using BaseCleanArchitecture.WebAPI.Middleware.AuditLogging;

namespace BaseCleanArchitecture.Tests.Middleware;

public class AuditErrorMessageTests
{
    [Fact]
    public void Message_Property_Is_Preferred()
    {
        Assert.Equal("x", AuditErrorMessage.FromResponse("{\"message\":\"x\",\"detail\":\"d\",\"title\":\"t\"}", "application/json"));
    }

    [Fact]
    public void Falls_Back_To_Detail_Then_Title()
    {
        Assert.Equal("d", AuditErrorMessage.FromResponse("{\"detail\":\"d\",\"title\":\"t\"}", "application/problem+json"));
        Assert.Equal("t", AuditErrorMessage.FromResponse("{\"title\":\"t\"}", "application/problem+json"));
    }

    [Fact]
    public void Property_Names_Are_Case_Insensitive()
    {
        Assert.Equal("x", AuditErrorMessage.FromResponse("{\"Message\":\"x\"}", "application/json"));
    }

    [Theory]
    [InlineData("{\"message\":", "application/json")] // truncated / broken JSON
    [InlineData("not json", "application/json")]
    [InlineData("[1,2]", "application/json")]
    [InlineData("{\"message\":123}", "application/json")]
    [InlineData("{\"message\":\"x\"}", "text/plain")]
    [InlineData(null, "application/json")]
    [InlineData("", "application/json")]
    public void Returns_Null_When_Nothing_Usable(string? text, string contentType)
    {
        Assert.Null(AuditErrorMessage.FromResponse(text, contentType));
    }

    [Fact]
    public void FromException_Uses_Type_Name_And_Message()
    {
        Assert.Equal("KeyNotFoundException: m", AuditErrorMessage.FromException(new KeyNotFoundException("m")));
    }

    [Fact]
    public void Long_Messages_Are_Capped_To_The_Column_Size()
    {
        var message = AuditErrorMessage.FromException(new Exception(new string('x', 10_000)));

        Assert.Equal(4000, message.Length);
    }
}
