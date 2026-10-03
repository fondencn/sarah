using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Sarah.API.BusinessObjects;
using Sarah.Persons.WebApi.Data;
using Sarah.Persons.WebApi.Data.Entities;

namespace Sarah.Persons.WebApi.Services;

public class NamedPositionService : INamedPositionService
{
    private const int CoordinatePrecision = 5;
    private static readonly TimeSpan CacheUsageRefreshInterval = TimeSpan.FromHours(1);

    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly ILogger<NamedPositionService> _logger;
    private readonly IConfiguration _configuration;

    public NamedPositionService(
        ApplicationDbContext dbContext,
        HttpClient httpClient,
        ILogger<NamedPositionService> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> ResolveNamedPositionAsync(LocatorPosition? position, CancellationToken cancellationToken = default)
    {
        if (position == null || !position.IsValid)
        {
            return "Unknown location";
        }

        decimal latitude = RoundCoordinate(position.Latitude.Value);
        decimal longitude = RoundCoordinate(position.Longtitude.Value);

        var cached = await _dbContext.NamedPositionCache
            .FirstOrDefaultAsync(x => x.Latitude == latitude && x.Longitude == longitude, cancellationToken);

        if (cached != null)
        {
            DateTime now = DateTime.UtcNow;
            if (cached.LastUsedAtUtc <= now - CacheUsageRefreshInterval)
            {
                cached.LastUsedAtUtc = now;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return cached.NamedLocation;
        }

        NominatimReverseResult? nominatimResult = await ResolveWithNominatimAsync(latitude, longitude, cancellationToken);
        if (string.IsNullOrWhiteSpace(nominatimResult?.DisplayName))
        {
            return FormatFallback(latitude, longitude);
        }

        string namedLocation = nominatimResult.DisplayName;

        var cacheEntry = new NamedPositionCacheEntity
        {
            Latitude = latitude,
            Longitude = longitude,
            NamedLocation = namedLocation,
            DisplayName = nominatimResult.DisplayName,
            NominatimJson = nominatimResult.RawJson,
            CreatedAtUtc = DateTime.UtcNow,
            LastUsedAtUtc = DateTime.UtcNow
        };

        _dbContext.NamedPositionCache.Add(cacheEntry);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(cacheEntry).State = EntityState.Detached;

            var existing = await _dbContext.NamedPositionCache
                .FirstOrDefaultAsync(x => x.Latitude == latitude && x.Longitude == longitude, cancellationToken);

            if (existing != null)
            {
                return existing.NamedLocation;
            }

            throw;
        }

        return namedLocation;
    }

    private async Task<NominatimReverseResult?> ResolveWithNominatimAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken)
    {
        try
        {
            string acceptLanguage = _configuration["Nominatim:AcceptLanguage"] ?? "en";
            string uri = string.Create(CultureInfo.InvariantCulture,
                $"/reverse?lat={latitude}&lon={longitude}&format=jsonv2&addressdetails=1&accept-language={Uri.EscapeDataString(acceptLanguage)}");

            using var response = await _httpClient.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nominatim reverse geocoding failed with status code {StatusCode}", response.StatusCode);
                return null;
            }

            string rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            NominatimReverseResponse? nominatimResponse = JsonSerializer.Deserialize<NominatimReverseResponse>(rawJson);
            if (nominatimResponse == null)
            {
                return null;
            }

            return new NominatimReverseResult
            {
                Response = nominatimResponse,
                DisplayName = nominatimResponse.DisplayName,
                RawJson = rawJson
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nominatim reverse geocoding request failed");
            return null;
        }
    }

    private static decimal RoundCoordinate(float value)
    {
        return decimal.Round((decimal)value, CoordinatePrecision, MidpointRounding.AwayFromZero);
    }

    private static string FormatFallback(decimal latitude, decimal longitude)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{latitude}, {longitude}");
    }

    private sealed class NominatimReverseResult
    {
        public string? DisplayName { get; init; }
        public string RawJson { get; init; } = string.Empty;
        public NominatimReverseResponse Response { get; init; } = new();
    }

    private sealed class NominatimReverseResponse
    {
        [JsonPropertyName("place_id")]
        public long? PlaceId { get; init; }

        [JsonPropertyName("licence")]
        public string? Licence { get; init; }

        [JsonPropertyName("osm_type")]
        public string? OsmType { get; init; }

        [JsonPropertyName("osm_id")]
        public long? OsmId { get; init; }

        [JsonPropertyName("lat")]
        public string? Lat { get; init; }

        [JsonPropertyName("lon")]
        public string? Lon { get; init; }

        [JsonPropertyName("category")]
        public string? Category { get; init; }

        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("place_rank")]
        public int? PlaceRank { get; init; }

        [JsonPropertyName("importance")]
        public double? Importance { get; init; }

        [JsonPropertyName("addresstype")]
        public string? AddressType { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; init; }

        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; init; }
    }

    private sealed class NominatimAddress
    {
        [JsonPropertyName("road")]
        public string? Road { get; init; }

        [JsonPropertyName("house_number")]
        public string? HouseNumber { get; init; }

        [JsonPropertyName("city")]
        public string? City { get; init; }

        [JsonPropertyName("town")]
        public string? Town { get; init; }

        [JsonPropertyName("village")]
        public string? Village { get; init; }

        [JsonPropertyName("state")]
        public string? State { get; init; }

        [JsonPropertyName("postcode")]
        public string? Postcode { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; init; }
    }
}
