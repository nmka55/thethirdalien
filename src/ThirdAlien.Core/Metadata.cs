namespace ThirdAlien.Core;

public enum MetadataField
{
    Title,
    Artist,
    Album,
    AlbumArtist,
    Genre,
    Composer,
    Lyrics,
    Copyright,
    Year,
    TrackNumber,
    TrackTotal,
    DiscNumber,
    DiscTotal,
    Website,
    Isrc,
}

public enum FieldIntent
{
    Keep,
    Set,
    Clear
}

public sealed record MetadataChange(MetadataField Field, FieldIntent Intent, string? Value = null)
{
    public static MetadataChange Keep(MetadataField field) => new(field, FieldIntent.Keep);

    public static MetadataChange Set(MetadataField field, string value) => new(field, FieldIntent.Set, value);

    public static MetadataChange Clear(MetadataField field) => new(field, FieldIntent.Clear);
}

public sealed record MetadataPatch(IReadOnlyList<MetadataChange> Changes)
{
    public static MetadataPatch Empty { get; } = new([]);

    public MetadataChange? For(MetadataField field) => Changes.LastOrDefault(change => change.Field == field);
}

public sealed record EditableMetadata(
    string? Title = null,
    string? Artist = null,
    string? Album = null,
    string? AlbumArtist = null,
    string? Genre = null,
    string? Composer = null,
    string? Lyrics = null,
    string? Copyright = null,
    string? Year = null,
    string? TrackNumber = null,
    string? TrackTotal = null,
    string? DiscNumber = null,
    string? DiscTotal = null,
    string? Website = null,
    string? Isrc = null,
    IReadOnlyDictionary<string, string>? AdditionalFields = null);

public sealed record AudioProperties(
    string Format,
    TimeSpan Duration,
    int BitrateKbps,
    int SampleRateHz,
    int Channels);

public sealed record AudioFileSnapshot(
    string Path,
    long Length,
    DateTimeOffset LastWriteTimeUtc,
    EditableMetadata Metadata,
    AudioProperties Properties,
    byte[]? FrontCover = null);
