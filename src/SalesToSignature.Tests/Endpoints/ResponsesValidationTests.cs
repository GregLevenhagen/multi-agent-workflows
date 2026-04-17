using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace SalesToSignature.Tests.Endpoints;

public class ResponsesValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ResponsesValidationTests(WebApplicationFactory<Program> factory)
    {
        // Register a mock IChatClient so the endpoint proceeds past the 503 check
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("AZURE_AI_PROJECT_ENDPOINT", "");
            builder.UseSetting("AZURE_CONTENT_SAFETY_ENDPOINT", "");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(new Mock<IChatClient>().Object);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Post_Responses_InvalidJson_Returns_400()
    {
        var content = new StringContent("not valid json", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/responses", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Contains("Invalid JSON", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Post_Responses_MissingInput_Returns_400()
    {
        var json = JsonSerializer.Serialize(new { foo = "bar" });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/responses", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Contains("Missing 'input'", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Post_Responses_EmptyInput_Returns_400()
    {
        var json = JsonSerializer.Serialize(new { input = "   " });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/responses", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Contains("required", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Post_Responses_NullInput_Returns_400()
    {
        var json = "{\"input\": null}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/responses", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Responses_EmptyBody_Returns_400()
    {
        var content = new StringContent("", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/responses", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
