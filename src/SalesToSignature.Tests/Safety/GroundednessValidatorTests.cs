using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using SalesToSignature.Agents.Safety;
using Xunit;

namespace SalesToSignature.Tests.Safety;

public class GroundednessValidatorTests
{
    [Fact]
    public async Task ValidateGroundednessAsync_Grounded_ReturnsTrue()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            ungroundedDetected = false,
            ungroundedPercentage = 0.0,
            ungroundedDetails = Array.Empty<object>()
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var validator = new GroundednessValidator("https://test.cognitiveservices.azure.com", "test-key", httpClient);

        var result = await validator.ValidateGroundednessAsync(
            "Acme Corp needs 3 senior .NET developers",
            "Acme Corp RFP requesting 3 senior .NET developers for Azure migration");

        Assert.True(result.IsGrounded);
        Assert.Empty(result.UngroundedSegments);
    }

    [Fact]
    public async Task ValidateGroundednessAsync_Ungrounded_ReturnsFalseWithSegments()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            ungroundedDetected = true,
            ungroundedPercentage = 0.35,
            ungroundedDetails = new[]
            {
                new { text = "The client has 500 employees worldwide" }
            },
            reasoning = "The claim about 500 employees is not supported by the source documents"
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var validator = new GroundednessValidator("https://test.cognitiveservices.azure.com", "test-key", httpClient);

        var result = await validator.ValidateGroundednessAsync(
            "The client has 500 employees worldwide and needs cloud migration",
            "Acme Corp RFP for cloud migration services");

        Assert.False(result.IsGrounded);
        Assert.Single(result.UngroundedSegments);
        Assert.Contains("500 employees", result.UngroundedSegments[0]);
    }

    [Fact]
    public async Task ValidateGroundednessAsync_ApiUnavailable_ReturnsSafeDefault()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handler.Object);
        var validator = new GroundednessValidator("https://test.cognitiveservices.azure.com", "test-key", httpClient);

        var result = await validator.ValidateGroundednessAsync("Test response", "Test source");

        Assert.True(result.IsGrounded);
        Assert.Contains("unavailable", result.Reasoning);
    }

    [Fact]
    public async Task ValidateGroundednessAsync_MultipleUngroundedSegments_ReturnsAll()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            ungroundedDetected = true,
            ungroundedPercentage = 0.60,
            ungroundedDetails = new[]
            {
                new { text = "The client has 500 employees worldwide" },
                new { text = "They have offices in 12 countries" },
                new { text = "Annual revenue exceeds $1B" }
            },
            reasoning = "Multiple claims are not supported by source documents"
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var validator = new GroundednessValidator("https://test.cognitiveservices.azure.com", "test-key", httpClient);

        var result = await validator.ValidateGroundednessAsync("Multi-claim response", "Acme Corp RFP");

        Assert.False(result.IsGrounded);
        Assert.Equal(3, result.UngroundedSegments.Count);
        Assert.Equal("Multiple claims are not supported by source documents", result.Reasoning);
    }

    [Fact]
    public async Task ValidateGroundednessAsync_UngroundedWithoutReasoningField_GeneratesDefaultReasoning()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            ungroundedDetected = true,
            ungroundedPercentage = 0.25,
            ungroundedDetails = new[]
            {
                new { text = "Ungrounded claim" }
            }
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var validator = new GroundednessValidator("https://test.cognitiveservices.azure.com", "test-key", httpClient);

        var result = await validator.ValidateGroundednessAsync("Response with claim", "Source data");

        Assert.False(result.IsGrounded);
        Assert.Contains("25", result.Reasoning);
    }

    [Fact]
    public async Task ValidateGroundednessAsync_NonSuccessStatus_ReturnsSafeDefault()
    {
        var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "{}");
        var validator = new GroundednessValidator("https://test.cognitiveservices.azure.com", "test-key", httpClient, maxRetries: 0);

        var result = await validator.ValidateGroundednessAsync("Test response", "Test source");

        Assert.True(result.IsGrounded);
        Assert.Contains("unavailable", result.Reasoning);
    }

    [Fact]
    public async Task ValidateGroundednessAsync_GroundedWithReasoningField_UsesApiReasoning()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            ungroundedDetected = false,
            ungroundedPercentage = 0.0,
            ungroundedDetails = Array.Empty<object>(),
            reasoning = "All claims are well-supported by the source documents"
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var validator = new GroundednessValidator("https://test.cognitiveservices.azure.com", "test-key", httpClient);

        var result = await validator.ValidateGroundednessAsync("Grounded response", "Source data");

        Assert.True(result.IsGrounded);
        Assert.Equal("All claims are well-supported by the source documents", result.Reasoning);
    }

    private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });

        return new HttpClient(handler.Object);
    }

    [Fact]
    public async Task ValidateGroundednessAsync_MultipleSources_ConcatenatesWithSeparator()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            ungroundedDetected = false,
            ungroundedPercentage = 0.0,
            reasoning = "Response is grounded in multiple sources"
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var validator = new GroundednessValidator("https://test.cognitiveservices.azure.com", "test-key", httpClient);

        var sources = new List<string> { "Source 1: Acme Corp RFP", "Source 2: Rate card data", "Source 3: Qualification notes" };
        var result = await validator.ValidateGroundednessAsync(
            "The budget is $500K-$750K for 12 months",
            sources);

        Assert.True(result.IsGrounded);
        Assert.Equal("Response is grounded in multiple sources", result.Reasoning);
    }

    [Fact]
    public async Task ValidateGroundednessAsync_EmptySourceList_HandledGracefully()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            ungroundedDetected = false,
            ungroundedPercentage = 0.0
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
        var validator = new GroundednessValidator("https://test.cognitiveservices.azure.com", "test-key", httpClient);

        var result = await validator.ValidateGroundednessAsync("Test response", Array.Empty<string>());

        Assert.True(result.IsGrounded);
    }

    [Fact]
    public void GroundednessResult_RecordEquality()
    {
        var a = new GroundednessResult(true, [], "Grounded");
        var b = new GroundednessResult(true, [], "Grounded");
        // Different list references but same content — records with lists use reference equality
        Assert.NotEqual(a, b); // Known behavior: List<T> uses reference equality

        var shared = new List<string> { "seg1" };
        var c = new GroundednessResult(false, shared, "Not grounded");
        var d = new GroundednessResult(false, shared, "Not grounded");
        Assert.Equal(c, d); // Same list reference → equal
    }
}
