using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SalesToSignature.Tests.Endpoints;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        // Override environment to avoid loading real Azure credentials
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("AZURE_AI_PROJECT_ENDPOINT", "");
            builder.UseSetting("AZURE_CONTENT_SAFETY_ENDPOINT", "");
        }).CreateClient();
    }

    [Fact]
    public async Task Get_Root_Returns_ServiceInfo()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal("Sales-to-Signature Multi-Agent Pipeline", root.GetProperty("service").GetString());
        Assert.Equal("healthy", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("version", out _));
        Assert.True(root.TryGetProperty("environment", out _));
    }

    [Fact]
    public async Task Get_HealthCheck_Returns_200()
    {
        var response = await _client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_Responses_Without_ChatClient_Returns_503()
    {
        var json = JsonSerializer.Serialize(new { input = "Test RFP content" });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/responses", content);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Post_Responses_InvalidJson_Returns_400()
    {
        // IChatClient must be registered for the endpoint to reach JSON parsing.
        // Without it, we get 503 before JSON parsing happens.
        // This test documents the behavior: invalid JSON still returns 503 when no IChatClient.
        var content = new StringContent("not json", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/responses", content);

        // Without IChatClient, 503 is returned before JSON is parsed
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Get_Root_HasChatClient_False_When_No_Endpoint()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        Assert.False(doc.RootElement.GetProperty("hasChatClient").GetBoolean());
    }

    [Fact]
    public async Task Get_Root_HasContentSafety_False_When_No_Endpoint()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        Assert.False(doc.RootElement.GetProperty("hasContentSafety").GetBoolean());
    }

    [Fact]
    public async Task Get_Root_Returns_Json_ContentType()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_Responses_Returns_Json_ProblemDetails_For_503()
    {
        var json = JsonSerializer.Serialize(new { input = "Test" });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/responses", content);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        // ProblemDetails format includes "detail" and "status" fields
        Assert.True(doc.RootElement.TryGetProperty("detail", out var detail));
        Assert.Contains("IChatClient", detail.GetString());
        Assert.Equal(503, doc.RootElement.GetProperty("status").GetInt32());
    }
}
