using Azure;
using Azure.AI.ContentSafety;
using Moq;
using SalesToSignature.Agents.Configuration;
using SalesToSignature.Agents.Safety;
using Xunit;

namespace SalesToSignature.Tests.Safety;

public class ContentSafetyFilterTests
{
    [Fact]
    public async Task AnalyzeTextAsync_SafeContent_NotBlocked()
    {
        var mockClient = new Mock<ContentSafetyClient>();

        var result = ContentSafetyModelFactory.AnalyzeTextResult(
            blocklistsMatch: [],
            categoriesAnalysis:
            [
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 0),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.SelfHarm, 0),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Sexual, 0),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Violence, 0)
            ]);

        mockClient.Setup(c => c.AnalyzeTextAsync(
                It.IsAny<AnalyzeTextOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(result, Mock.Of<Response>()));

        var filter = new ContentSafetyFilter(mockClient.Object);
        var safetyResult = await filter.AnalyzeTextAsync("We need 3 developers for Azure migration");

        Assert.False(safetyResult.IsBlocked);
        Assert.Equal(4, safetyResult.Categories.Count);
        Assert.All(safetyResult.Categories.Values, severity => Assert.Equal(0, severity));
    }

    [Fact]
    public async Task AnalyzeTextAsync_HarmfulContent_Blocked()
    {
        var mockClient = new Mock<ContentSafetyClient>();

        var result = ContentSafetyModelFactory.AnalyzeTextResult(
            blocklistsMatch: [],
            categoriesAnalysis:
            [
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 6),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.SelfHarm, 0),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Sexual, 0),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Violence, 2)
            ]);

        mockClient.Setup(c => c.AnalyzeTextAsync(
                It.IsAny<AnalyzeTextOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(result, Mock.Of<Response>()));

        var filter = new ContentSafetyFilter(mockClient.Object);
        var safetyResult = await filter.AnalyzeTextAsync("Some harmful content");

        Assert.True(safetyResult.IsBlocked);
        Assert.Equal(6, safetyResult.Categories["Hate"]);
    }

    [Fact]
    public async Task AnalyzeTextAsync_CustomThreshold_RespectedForBlocking()
    {
        var mockClient = new Mock<ContentSafetyClient>();

        var result = ContentSafetyModelFactory.AnalyzeTextResult(
            blocklistsMatch: [],
            categoriesAnalysis:
            [
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 2),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.SelfHarm, 0),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Sexual, 0),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Violence, 0)
            ]);

        mockClient.Setup(c => c.AnalyzeTextAsync(
                It.IsAny<AnalyzeTextOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(result, Mock.Of<Response>()));

        // Low threshold of 2 — severity of 2 should trigger blocking
        var settings = new AgentSettings { ContentSafetySeverityThreshold = 2 };
        var filter = new ContentSafetyFilter(mockClient.Object, settings);
        var safetyResult = await filter.AnalyzeTextAsync("Mildly concerning content");

        Assert.True(safetyResult.IsBlocked);
    }

    [Fact]
    public async Task AnalyzeTextAsync_SeverityAtExactThreshold_Blocked()
    {
        var mockClient = new Mock<ContentSafetyClient>();

        var result = ContentSafetyModelFactory.AnalyzeTextResult(
            blocklistsMatch: [],
            categoriesAnalysis:
            [
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 0),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.SelfHarm, 0),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Sexual, 0),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Violence, 4)
            ]);

        mockClient.Setup(c => c.AnalyzeTextAsync(
                It.IsAny<AnalyzeTextOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(result, Mock.Of<Response>()));

        // Default threshold is 4 — severity exactly at threshold should block
        var filter = new ContentSafetyFilter(mockClient.Object);
        var safetyResult = await filter.AnalyzeTextAsync("Content at threshold");

        Assert.True(safetyResult.IsBlocked);
        Assert.Equal(4, safetyResult.Categories["Violence"]);
    }

    [Fact]
    public async Task AnalyzeTextAsync_SeverityBelowThreshold_NotBlocked()
    {
        var mockClient = new Mock<ContentSafetyClient>();

        var result = ContentSafetyModelFactory.AnalyzeTextResult(
            blocklistsMatch: [],
            categoriesAnalysis:
            [
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 2),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.SelfHarm, 0),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Sexual, 2),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Violence, 2)
            ]);

        mockClient.Setup(c => c.AnalyzeTextAsync(
                It.IsAny<AnalyzeTextOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(result, Mock.Of<Response>()));

        // Default threshold is 4 — severity of 2 should not block
        var filter = new ContentSafetyFilter(mockClient.Object);
        var safetyResult = await filter.AnalyzeTextAsync("Mildly flagged content");

        Assert.False(safetyResult.IsBlocked);
    }

    [Fact]
    public async Task AnalyzeTextAsync_HighThreshold_NeverBlocks()
    {
        var mockClient = new Mock<ContentSafetyClient>();

        var result = ContentSafetyModelFactory.AnalyzeTextResult(
            blocklistsMatch: [],
            categoriesAnalysis:
            [
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 6),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.SelfHarm, 4),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Sexual, 4),
                ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Violence, 6)
            ]);

        mockClient.Setup(c => c.AnalyzeTextAsync(
                It.IsAny<AnalyzeTextOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(result, Mock.Of<Response>()));

        // Very high threshold — nothing should trigger
        var settings = new AgentSettings { ContentSafetySeverityThreshold = 8 };
        var filter = new ContentSafetyFilter(mockClient.Object, settings);
        var safetyResult = await filter.AnalyzeTextAsync("Flagged but below high threshold");

        Assert.False(safetyResult.IsBlocked);
    }

    [Fact]
    public async Task AnalyzeTextAsync_ApiFailure_ReturnsSafeDefault()
    {
        var mockClient = new Mock<ContentSafetyClient>();

        mockClient.Setup(c => c.AnalyzeTextAsync(
                It.IsAny<AnalyzeTextOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(503, "Service Unavailable"));

        var filter = new ContentSafetyFilter(mockClient.Object);
        var safetyResult = await filter.AnalyzeTextAsync("Test content");

        Assert.False(safetyResult.IsBlocked);
        Assert.Empty(safetyResult.Categories);
    }

    [Fact]
    public async Task AnalyzeLongTextAsync_ShortText_DelegatesToAnalyzeText()
    {
        var mockClient = new Mock<ContentSafetyClient>();
        var mockResponse = ContentSafetyModelFactory.AnalyzeTextResult(
            categoriesAnalysis: [ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 0)]);

        mockClient.Setup(c => c.AnalyzeTextAsync(
                It.IsAny<AnalyzeTextOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(mockResponse, new Mock<Response>().Object));

        var filter = new ContentSafetyFilter(mockClient.Object);
        var result = await filter.AnalyzeLongTextAsync("Short text", maxChunkSize: 10_000);

        Assert.False(result.IsBlocked);
        // AnalyzeTextAsync should be called exactly once (not chunked)
        mockClient.Verify(c => c.AnalyzeTextAsync(
            It.IsAny<AnalyzeTextOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeLongTextAsync_LongText_SplitsIntoChunks()
    {
        var mockClient = new Mock<ContentSafetyClient>();
        var mockResponse = ContentSafetyModelFactory.AnalyzeTextResult(
            categoriesAnalysis: [ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 0)]);

        mockClient.Setup(c => c.AnalyzeTextAsync(
                It.IsAny<AnalyzeTextOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(mockResponse, new Mock<Response>().Object));

        var filter = new ContentSafetyFilter(mockClient.Object);
        var longText = new string('A', 25); // 25 chars with chunk size 10 = 3 chunks
        var result = await filter.AnalyzeLongTextAsync(longText, maxChunkSize: 10);

        Assert.False(result.IsBlocked);
        // Should have been called 3 times (10 + 10 + 5 chars)
        mockClient.Verify(c => c.AnalyzeTextAsync(
            It.IsAny<AnalyzeTextOptions>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public void SafetyResult_RecordEquality()
    {
        var cats = new Dictionary<string, int> { ["Hate"] = 0, ["Violence"] = 2 };
        var a = new SafetyResult(false, cats);
        var b = new SafetyResult(false, cats);
        Assert.Equal(a, b); // Same dictionary reference → equal
    }
}
