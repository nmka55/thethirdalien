namespace ThirdAlien.Core;

public sealed record CatalogAlbum(
    long CollectionId,
    string Name,
    string Artist,
    DateOnly? ReleaseDate,
    int? TrackCount,
    int? DiscCount,
    string? Genre,
    string? Copyright,
    string? ArtworkUrl,
    bool IsExplicit,
    string Storefront);

public sealed record CatalogTrack(
    long TrackId,
    long CollectionId,
    string Name,
    string Artist,
    string Album,
    string? AlbumArtist,
    int? TrackNumber,
    int? TrackTotal,
    int? DiscNumber,
    int? DiscTotal,
    DateOnly? ReleaseDate,
    string? Genre,
    string? Copyright,
    TimeSpan? Duration,
    string? Isrc,
    bool IsExplicit);

public sealed record CatalogAlbumDetail(CatalogAlbum Album, IReadOnlyList<CatalogTrack> Tracks);

public interface ICatalogProvider
{
    Task<IReadOnlyList<CatalogAlbum>> SearchAlbumsAsync(
        string term,
        string storefront,
        bool includeExplicit,
        CancellationToken cancellationToken = default);

    Task<byte[]?> DownloadArtworkAsync(string artworkUrl, CancellationToken cancellationToken = default);

    Task<CatalogAlbumDetail> GetAlbumAsync(
        long collectionId,
        string storefront,
        CancellationToken cancellationToken = default);
}
