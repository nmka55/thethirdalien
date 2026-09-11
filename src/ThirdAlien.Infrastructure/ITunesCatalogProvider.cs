using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ThirdAlien.Core;

namespace ThirdAlien.Infrastructure;

public sealed class ITunesCatalogProvider(HttpClient httpClient) : ICatalogProvider
{
    private const int SearchLimit = 70;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<CatalogAlbum>> SearchAlbumsAsync(
        string term,
        string storefront,
        bool includeExplicit,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(term);
        var country = NormalizeStorefront(storefront);
        var normalizedTerm = term.Trim();
        var query = new Dictionary<string, string>
        {
            ["term"] = normalizedTerm,
            ["media"] = "music",
            ["entity"] = "album",
            ["limit"] = SearchLimit.ToString(CultureInfo.InvariantCulture),
            ["country"] = country,
            ["explicit"] = includeExplicit ? "yes" : "no"
        };

        // This is the same narrow catalog scope used by the original application:
        // music collections returned as albums. It cannot return podcasts or raw tracks.
        var albumResponse = await GetAsync("search", query, cancellationToken);
        var albums = albumResponse.Results
            .Where(result =>
                string.Equals(result.WrapperType, "collection", StringComparison.OrdinalIgnoreCase)
                && string.Equals(result.CollectionType, "Album", StringComparison.OrdinalIgnoreCase)
                && result.CollectionId is not null
                && !string.IsNullOrWhiteSpace(result.CollectionName))
            .Select(result => ToAlbum(result, country));

        // Apple occasionally omits a well-known album from an album search. In that
        // specific case, look only for a song whose collection title exactly matches
        // the user's query, then surface its collection as an album—not the song.
        var albumList = albums.ToList();
        if (!albumList.Any(album => string.Equals(album.Name, normalizedTerm, StringComparison.OrdinalIgnoreCase)))
        {
            var exactTitleSongResponse = await GetAsync("search", new Dictionary<string, string>
            {
                ["term"] = normalizedTerm,
                ["media"] = "music",
                ["entity"] = "song",
                ["limit"] = "200",
                ["country"] = country,
                ["explicit"] = includeExplicit ? "yes" : "no"
            }, cancellationToken);

            albumList.AddRange(exactTitleSongResponse.Results
                .Where(result =>
                    string.Equals(result.Kind, "song", StringComparison.OrdinalIgnoreCase)
                    && result.CollectionId is not null
                    && !string.IsNullOrWhiteSpace(result.CollectionName)
                    && string.Equals(result.CollectionName.Trim(), normalizedTerm, StringComparison.OrdinalIgnoreCase))
                .Select(result => ToAlbum(result, country)));
        }

        return albumList
            .DistinctBy(album => album.CollectionId)
            .OrderByDescending(album => string.Equals(album.Name, normalizedTerm, StringComparison.OrdinalIgnoreCase))
            .ThenBy(album => album.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(album => album.Artist, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    public async Task<CatalogAlbumDetail> GetAlbumAsync(
        long collectionId,
        string storefront,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(collectionId);
        var country = NormalizeStorefront(storefront);
        var query = new Dictionary<string, string>
        {
            ["id"] = collectionId.ToString(CultureInfo.InvariantCulture),
            ["entity"] = "song",
            ["limit"] = "200",
            ["country"] = country
        };

        var response = await GetAsync("lookup", query, cancellationToken);
        var collection = response.Results.FirstOrDefault(result =>
            string.Equals(result.WrapperType, "collection", StringComparison.OrdinalIgnoreCase)
            && result.CollectionId == collectionId)
            ?? throw new CatalogNotFoundException($"Apple did not return album {collectionId} for storefront {country}.");

        var album = ToAlbum(collection, country);
        var tracks = response.Results
            .Where(result => string.Equals(result.Kind, "song", StringComparison.OrdinalIgnoreCase)
                && result.TrackId is not null)
            .Select(result => ToTrack(result, album))
            .OrderBy(track => track.DiscNumber)
            .ThenBy(track => track.TrackNumber)
            .ToList();

        return new(album, tracks);
    }

    public async Task<byte[]?> DownloadArtworkAsync(string artworkUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artworkUrl);
        if (!Uri.TryCreate(artworkUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new CatalogResponseException("Apple returned an invalid artwork URL.");
        }

        using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null && !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new CatalogResponseException("Apple artwork response was not an image.");
        }

        var length = response.Content.Headers.ContentLength;
        if (length is > 15 * 1024 * 1024)
        {
            throw new CatalogResponseException("Apple artwork response exceeds the 15 MB safety limit.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length == 0 || bytes.Length > 15 * 1024 * 1024)
        {
            throw new CatalogResponseException("Apple artwork response was empty or exceeded the 15 MB safety limit.");
        }

        return bytes;
    }
    private async Task<ITunesResponse> GetAsync(string endpoint, IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken)
    {
        var queryString = string.Join("&", query.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}?{queryString}");
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new CatalogRateLimitedException("Apple temporarily limited catalog requests. Try again shortly.");
        }

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<ITunesResponse>(stream, JsonOptions, cancellationToken);
        return payload ?? throw new CatalogResponseException("Apple returned an empty catalog response.");
    }

    private static CatalogAlbum ToAlbum(ITunesResult result, string storefront) => new(
        result.CollectionId!.Value,
        result.CollectionName ?? "Unknown album",
        result.ArtistName ?? "Unknown artist",
        ParseDate(result.ReleaseDate),
        result.TrackCount,
        result.DiscCount,
        result.PrimaryGenreName,
        result.Copyright,
        RequestArtworkSize(result.ArtworkUrl100, 600),
        string.Equals(result.CollectionExplicitness, "explicit", StringComparison.OrdinalIgnoreCase),
        storefront);

    private static CatalogTrack ToTrack(ITunesResult result, CatalogAlbum album) => new(
        result.TrackId!.Value,
        result.CollectionId ?? album.CollectionId,
        result.TrackName ?? "Unknown track",
        result.ArtistName ?? album.Artist,
        result.CollectionName ?? album.Name,
        album.Artist,
        result.TrackNumber,
        result.TrackCount ?? album.TrackCount,
        result.DiscNumber,
        result.DiscCount ?? album.DiscCount,
        ParseDate(result.ReleaseDate),
        result.PrimaryGenreName ?? album.Genre,
        result.Copyright ?? album.Copyright,
        result.TrackTimeMillis is null ? null : TimeSpan.FromMilliseconds(result.TrackTimeMillis.Value),
        null,
        string.Equals(result.TrackExplicitness, "explicit", StringComparison.OrdinalIgnoreCase));

    private static string? RequestArtworkSize(string? artworkUrl, int size)
    {
        if (string.IsNullOrWhiteSpace(artworkUrl))
        {
            return null;
        }

        var requestedSize = $"{size}x{size}";
        return artworkUrl
            .Replace("100x100bb", $"{requestedSize}bb", StringComparison.OrdinalIgnoreCase)
            .Replace("100x100", requestedSize, StringComparison.OrdinalIgnoreCase);
    }
    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date) ? date : null;

    private static string NormalizeStorefront(string storefront)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storefront);
        var normalized = storefront.Trim().ToUpperInvariant();
        if (normalized.Length != 2 || !normalized.All(char.IsAsciiLetter))
        {
            throw new ArgumentException("Storefront must be a two-letter country code.", nameof(storefront));
        }

        return normalized;
    }

    private sealed record ITunesResponse
    {
        [JsonPropertyName("results")]
        public List<ITunesResult> Results { get; init; } = [];
    }

    private sealed record ITunesResult
    {
        public string? WrapperType { get; init; }
        public string? Kind { get; init; }
        public long? CollectionId { get; init; }
        public long? ArtistId { get; init; }
        public long? TrackId { get; init; }
        public string? ArtistName { get; init; }
        public string? CollectionName { get; init; }
        public string? CollectionType { get; init; }
        public string? TrackName { get; init; }
        public string? ArtworkUrl100 { get; init; }
        public string? ReleaseDate { get; init; }
        public int? TrackCount { get; init; }
        public int? DiscCount { get; init; }
        public int? TrackNumber { get; init; }
        public int? DiscNumber { get; init; }
        public int? TrackTimeMillis { get; init; }
        public string? PrimaryGenreName { get; init; }
        public string? Copyright { get; init; }
        public string? CollectionExplicitness { get; init; }
        public string? TrackExplicitness { get; init; }
    }
}

public sealed class CatalogNotFoundException(string message) : Exception(message);

public sealed class CatalogRateLimitedException(string message) : Exception(message);

public sealed class CatalogResponseException(string message) : Exception(message);
