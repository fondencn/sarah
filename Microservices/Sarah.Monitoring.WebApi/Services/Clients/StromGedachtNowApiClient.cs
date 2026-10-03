using System.CodeDom.Compiler;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Sarah.Monitoring.Clients;

/// <summary>
/// Minimal OpenAPI-based client for StromGedacht /v1/now.
/// Swagger source: https://api.stromgedacht.de/swagger/v1/swagger.json
/// </summary>
[GeneratedCode("OpenAPI", "v1")]
public sealed class StromGedachtNowApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StromGedachtNowApiClient> _logger;

    public StromGedachtNowApiClient(HttpClient httpClient, ILogger<StromGedachtNowApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<StromGedachtNowResponse> GetNowAsync(
        string zip,
        int? hoursInFuture = null,
        string? b2bId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(zip))
        {
            throw new ArgumentException("zip must not be empty", nameof(zip));
        }

        var queryParts = new List<string>
        {
            "zip=" + Uri.EscapeDataString(zip)
        };

        if (hoursInFuture.HasValue)
        {
            queryParts.Add("hoursInFuture=" + hoursInFuture.Value.ToString(CultureInfo.InvariantCulture));
        }

        string relativeUrl = "/v1/now?" + string.Join("&", queryParts);
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        if (!string.IsNullOrWhiteSpace(b2bId))
        {
            request.Headers.Add("X-B2B-ID", b2bId);
        }

        _logger.LogDebug("Requesting StromGedacht now state for zip {Zip}", zip);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<StromGedachtNowResponse>(cancellationToken: cancellationToken);
        if (payload == null)
        {
            throw new InvalidOperationException("StromGedacht now response was empty.");
        }

        return payload;
    }
}

[GeneratedCode("OpenAPI", "v1")]
public sealed class StromGedachtNowResponse
{
    [JsonPropertyName("state")]
    public int State { get; set; }
}
