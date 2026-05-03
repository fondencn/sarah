using System.CodeDom.Compiler;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Sarah.Rules.Services.Clients;

/// <summary>
/// OpenAPI-based client for StromGedacht grid state forecast endpoints.
/// Swagger source: https://api.stromgedacht.de/swagger/v1/swagger.json
/// </summary>
[GeneratedCode("OpenAPI", "v1")]
public sealed class StromGedachtGridStatesApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StromGedachtGridStatesApiClient> _logger;

    public StromGedachtGridStatesApiClient(HttpClient httpClient, ILogger<StromGedachtGridStatesApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<StromGedachtGridStateRangeResponse> GetStatesRelativeAsync(
        string zip,
        int hoursInFuture,
        int hoursInPast = 0,
        string? b2bId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(zip))
        {
            throw new ArgumentException("zip must not be empty", nameof(zip));
        }

        if (hoursInFuture < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hoursInFuture), "hoursInFuture must be >= 0");
        }

        if (hoursInPast < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hoursInPast), "hoursInPast must be >= 0");
        }

        string relativeUrl = "/v1/statesRelative"
            + "?zip=" + Uri.EscapeDataString(zip)
            + "&hoursInFuture=" + hoursInFuture.ToString(CultureInfo.InvariantCulture)
            + "&hoursInPast=" + hoursInPast.ToString(CultureInfo.InvariantCulture);

        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        if (!string.IsNullOrWhiteSpace(b2bId))
        {
            request.Headers.Add("X-B2B-ID", b2bId);
        }

        _logger.LogDebug("Requesting StromGedacht statesRelative for zip {Zip} (future={HoursInFuture}, past={HoursInPast})",
            zip, hoursInFuture, hoursInPast);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<StromGedachtGridStateRangeResponse>(cancellationToken: cancellationToken);
        return payload ?? new StromGedachtGridStateRangeResponse();
    }
}

[GeneratedCode("OpenAPI", "v1")]
public sealed class StromGedachtGridStateRangeResponse
{
    [JsonPropertyName("states")]
    public List<StromGedachtGridStateWindow>? States { get; set; }
}

[GeneratedCode("OpenAPI", "v1")]
public sealed class StromGedachtGridStateWindow
{
    [JsonPropertyName("from")]
    public DateTimeOffset? From { get; set; }

    [JsonPropertyName("to")]
    public DateTimeOffset? To { get; set; }

    [JsonPropertyName("state")]
    public int State { get; set; }
}
