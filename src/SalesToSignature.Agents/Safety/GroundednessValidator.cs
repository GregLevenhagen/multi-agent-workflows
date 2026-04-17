using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace SalesToSignature.Agents.Safety;

public record GroundednessResult(bool IsGrounded, List<string> UngroundedSegments, string Reasoning);

public class GroundednessValidator
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _domain;
    private readonly int _maxRetries;
    private static readonly ActivitySource ActivitySource = new("SalesToSignature.Agents");

    /// <param name="domain">Detection domain: "Generic" (default), "Medical", or "Financial".</param>
    public GroundednessValidator(string endpoint, string apiKey, HttpClient? httpClient = null, string domain = "Generic", int maxRetries = 3)
    {
        _endpoint = endpoint.TrimEnd('/');
        _apiKey = apiKey;
        _httpClient = httpClient ?? new HttpClient();
        _domain = domain;
        _maxRetries = maxRetries;
    }

    /// <summary>
    /// Validates that a response is grounded in the provided source documents.
    /// Uses the Groundedness Detection API to check for hallucinated or ungrounded content.
    /// </summary>
    public virtual async Task<GroundednessResult> ValidateGroundednessAsync(
        string response,
        string groundingSources,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("Groundedness.Validate");

        try
        {
            var requestBody = new
            {
                domain = _domain,
                task = "QnA",
                text = response,
                groundingSources = groundingSources,
                reasoning = true
            };

            using var httpResponse = await SendWithRetryAsync(requestBody, cancellationToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                var content = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var isUngrounded = root.GetProperty("ungroundedDetected").GetBoolean();
                var ungroundedPercentage = root.TryGetProperty("ungroundedPercentage", out var pct)
                    ? pct.GetDouble() : 0.0;

                var ungroundedSegments = new List<string>();
                if (root.TryGetProperty("ungroundedDetails", out var details))
                {
                    foreach (var detail in details.EnumerateArray())
                    {
                        if (detail.TryGetProperty("text", out var text))
                            ungroundedSegments.Add(text.GetString() ?? "");
                    }
                }

                var reasoning = root.TryGetProperty("reasoning", out var reasoningProp)
                    ? reasoningProp.GetString() ?? ""
                    : isUngrounded ? $"Response contains {ungroundedPercentage:P0} ungrounded content" : "Response is grounded in source data";

                var result = new GroundednessResult(!isUngrounded, ungroundedSegments, reasoning);

                activity?.SetTag("groundedness.is_grounded", result.IsGrounded);
                activity?.SetTag("groundedness.ungrounded_segments", result.UngroundedSegments.Count);
                return result;
            }

            activity?.SetTag("groundedness.fallback", true);
            return new GroundednessResult(true, [], "Groundedness API unavailable — assuming grounded");
        }
        catch (HttpRequestException)
        {
            activity?.SetTag("groundedness.fallback", true);
            return new GroundednessResult(true, [], "Groundedness API unavailable — assuming grounded");
        }
    }

    /// <summary>
    /// Validates groundedness against multiple source documents.
    /// Concatenates sources with separator markers for the API.
    /// </summary>
    public virtual async Task<GroundednessResult> ValidateGroundednessAsync(
        string response,
        IReadOnlyList<string> groundingSources,
        CancellationToken cancellationToken = default)
    {
        var combined = string.Join("\n\n---SOURCE BOUNDARY---\n\n", groundingSources);
        return await ValidateGroundednessAsync(response, combined, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(object requestBody, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_endpoint}/contentsafety/text:detectGroundedness?api-version=2024-09-15-preview");
            request.Content = JsonContent.Create(requestBody);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            var statusCode = (int)response.StatusCode;
            var isTransient = statusCode == 429 || statusCode >= 500;

            if (!isTransient || attempt >= _maxRetries)
                return response;

            response.Dispose();
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            await Task.Delay(delay, cancellationToken);
        }
    }
}
