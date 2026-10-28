using System.Net;
using System.Text;
using System.Text.Json;
using BaseCleanArchitecture.Domain.Enums;
using BaseCleanArchitecture.Tests.TestSupport;

namespace BaseCleanArchitecture.Tests.Middleware;

public class AuditLoggingMiddlewareTests
{
    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Successful_Post_Is_Logged_As_Activity_With_Encrypted_Bodies()
    {
        await using var server = await AuditTestServer.StartAsync();

        var response = await server.Client.PostAsync("/echo", Json("{\"name\":\"abc\"}"));
        var log = await server.WaitForLogAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AuditType.Activity, log.AuditType);
        Assert.Equal(200, log.StatusCode);
        Assert.Equal("POST", log.HttpMethod);
        Assert.Equal("/echo", log.Path);
        Assert.True(log.DurationMs >= 0);
        Assert.Null(log.ErrorMessage);
        Assert.StartsWith("v1:", log.RequestBody);
        Assert.StartsWith("v1:", log.ResponseBody);
        Assert.DoesNotContain("abc", log.RequestBody);
        Assert.Equal("{\"name\":\"abc\"}", server.Encryptor.Decrypt(log.RequestBody!));
        Assert.Contains("\"length\":14", server.Encryptor.Decrypt(log.ResponseBody!));
        Assert.False(log.IsRequestTruncated);
        Assert.False(log.IsResponseTruncated);
    }

    [Fact]
    public async Task Client_Receives_Original_Response_Unchanged()
    {
        await using var server = await AuditTestServer.StartAsync();

        var response = await server.Client.PostAsync("/echo", Json("{\"name\":\"abc\"}"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("{\"length\":14,\"echo\":\"{\\u0022name\\u0022:\\u0022abc\\u0022}\"}", body);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Result_Failure_404_Is_Activity_With_ErrorMessage_From_Message()
    {
        await using var server = await AuditTestServer.StartAsync();

        var response = await server.Client.GetAsync("/result-404");
        var log = await server.WaitForLogAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(AuditType.Activity, log.AuditType);
        Assert.Equal(404, log.StatusCode);
        Assert.Equal("nope", log.ErrorMessage);
    }

    [Fact]
    public async Task Thrown_Exception_Is_Logged_As_Exception_With_Handler_Status_And_Message()
    {
        await using var server = await AuditTestServer.StartAsync();

        var response = await server.Client.GetAsync("/boom");
        var log = await server.WaitForLogAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // GlobalExceptionHandler maps KeyNotFoundException
        Assert.Equal(AuditType.Exception, log.AuditType);
        Assert.Equal(404, log.StatusCode);
        Assert.Equal("KeyNotFoundException: missing", log.ErrorMessage);
        Assert.Contains("Not Found", server.Encryptor.Decrypt(log.ResponseBody!));
    }

    [Fact]
    public async Task Unauthenticated_Request_Has_Null_UserId_And_Username()
    {
        await using var server = await AuditTestServer.StartAsync();

        await server.Client.GetAsync("/anything");
        var log = await server.WaitForLogAsync();

        Assert.Null(log.UserId);
        Assert.Null(log.Username);
    }

    [Fact]
    public async Task Authenticated_Request_Has_UserId_And_Username_Read_After_The_Pipeline_Ran()
    {
        await using var server = await AuditTestServer.StartAsync();

        await server.Client.GetAsync("/login"); // endpoint "authenticates" while handling the request
        var log = await server.WaitForLogAsync();

        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), log.UserId);
        Assert.Equal("alice", log.Username);
    }

    [Fact]
    public async Task Get_Without_Body_Has_Null_RequestBody()
    {
        await using var server = await AuditTestServer.StartAsync();

        await server.Client.GetAsync("/anything");
        var log = await server.WaitForLogAsync();

        Assert.Null(log.RequestBody);
        Assert.False(log.IsRequestTruncated);
    }

    [Fact]
    public async Task NoContent_204_Has_Null_ResponseBody()
    {
        await using var server = await AuditTestServer.StartAsync();

        var response = await server.Client.GetAsync("/nocontent");
        var log = await server.WaitForLogAsync();

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(204, log.StatusCode);
        Assert.Null(log.ResponseBody);
    }

    [Fact]
    public async Task Oversized_Request_Body_Is_Truncated_Flagged_And_Endpoint_Still_Reads_Full_Body()
    {
        await using var server = await AuditTestServer.StartAsync(maxBodyBytes: 100);
        var payload = new string('a', 2000);

        var response = await server.Client.PostAsync("/echo", new StringContent(payload, Encoding.UTF8, "text/plain"));
        var log = await server.WaitForLogAsync();

        using var echoed = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2000, echoed.RootElement.GetProperty("length").GetInt32());
        Assert.Equal(new string('a', 100), server.Encryptor.Decrypt(log.RequestBody!));
        Assert.True(log.IsRequestTruncated);
        Assert.True(log.IsResponseTruncated); // the 2000+ byte echo response is also over the 100 byte cap
    }

    [Fact]
    public async Task Binary_Response_Is_Stored_As_Placeholder()
    {
        await using var server = await AuditTestServer.StartAsync();

        var response = await server.Client.GetAsync("/binary");
        var log = await server.WaitForLogAsync();

        Assert.Equal(10, (await response.Content.ReadAsByteArrayAsync()).Length);
        Assert.Equal("[binary content, 10 bytes]", server.Encryptor.Decrypt(log.ResponseBody!));
        Assert.False(log.IsResponseTruncated);
    }

    [Fact]
    public async Task Binary_Request_Is_Stored_As_Placeholder_Without_Reading_It()
    {
        await using var server = await AuditTestServer.StartAsync();
        var content = new ByteArrayContent(new byte[500]);
        content.Headers.ContentType = new("application/octet-stream");

        await server.Client.PostAsync("/anything", content);
        var log = await server.WaitForLogAsync();

        Assert.Equal("[binary content, 500 bytes]", server.Encryptor.Decrypt(log.RequestBody!));
    }

    [Fact]
    public async Task Entity_Changes_From_AuditContext_Are_Serialized_And_Encrypted_Into_Changes()
    {
        await using var server = await AuditTestServer.StartAsync();

        await server.Client.GetAsync("/changes");
        var log = await server.WaitForLogAsync();

        var json = server.Encryptor.Decrypt(log.Changes!);
        using var doc = JsonDocument.Parse(json);
        var change = doc.RootElement[0];
        Assert.Equal("Category", change.GetProperty("Entity").GetString());
        Assert.Equal("Added", change.GetProperty("Action").GetString());
        Assert.Equal("Books", change.GetProperty("Properties").GetProperty("Name").GetProperty("New").GetString());
    }

    [Fact]
    public async Task Changes_Is_Null_When_Request_Changed_Nothing()
    {
        await using var server = await AuditTestServer.StartAsync();

        await server.Client.GetAsync("/anything");
        var log = await server.WaitForLogAsync();

        Assert.Null(log.Changes);
    }

    [Fact]
    public async Task Publisher_Refusing_The_Log_Does_Not_Break_The_Response()
    {
        await using var server = await AuditTestServer.StartAsync();
        server.PublisherAccepts = false;

        var response = await server.Client.GetAsync("/anything");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, server.PublishedCount);
    }

    [Fact]
    public async Task Encryptor_Failure_Does_Not_Break_Response_And_Nothing_Is_Published()
    {
        await using var server = await AuditTestServer.StartAsync();
        server.EncryptorThrows = true;

        var response = await server.Client.PostAsync("/echo", Json("{\"a\":1}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await Task.Delay(100);
        Assert.Equal(0, server.PublishedCount);
    }

    [Fact]
    public async Task Username_Longer_Than_The_Column_Is_Truncated()
    {
        await using var server = await AuditTestServer.StartAsync();
        server.User = new BaseCleanArchitecture.Application.Common.Models.CurrentUser { Id = Guid.NewGuid(), Username = new string('u', 300) };

        await server.Client.GetAsync("/anything");
        var log = await server.WaitForLogAsync();

        Assert.Equal(256, log.Username!.Length);
    }

    [Fact]
    public async Task Null_Characters_Are_Removed_From_Plaintext_Columns()
    {
        await using var server = await AuditTestServer.StartAsync();
        server.User = new BaseCleanArchitecture.Application.Common.Models.CurrentUser { Id = Guid.NewGuid(), Username = "al ice" };

        await server.Client.GetAsync("/nul");
        var log = await server.WaitForLogAsync();

        Assert.Equal("InvalidOperationException: badmessage", log.ErrorMessage);
        Assert.Equal("alice", log.Username);
        Assert.DoesNotContain(' ', log.Path);
    }

    [Fact]
    public async Task Huge_MaxBodyBytes_Setting_Does_Not_Break_Request_Capture()
    {
        await using var server = await AuditTestServer.StartAsync(maxBodyBytes: int.MaxValue);

        await server.Client.PostAsync("/echo", Json("{\"a\":1}"));
        var log = await server.WaitForLogAsync();

        Assert.Equal("{\"a\":1}", server.Encryptor.Decrypt(log.RequestBody!));
    }

    [Fact]
    public async Task Auth_Endpoint_Bodies_Are_Redacted()
    {
        await using var server = await AuditTestServer.StartAsync();

        await server.Client.PostAsync("/api/auth/login", Json("{\"email\":\"a@b.c\",\"password\":\"hunter2\"}"));
        var log = await server.WaitForLogAsync();

        Assert.Equal("[redacted]", server.Encryptor.Decrypt(log.RequestBody!));
    }
}
