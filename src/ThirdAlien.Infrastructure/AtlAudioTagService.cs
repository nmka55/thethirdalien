using System.ComponentModel.DataAnnotations;
using ATL;
using ThirdAlien.Core;

namespace ThirdAlien.Infrastructure;

public sealed class AtlAudioTagService : IAudioTagService
{
    public Task<AudioFileSnapshot> ReadAsync(string path, CancellationToken cancellationToken = default) =>
        Task.Run(() => Read(path, cancellationToken), cancellationToken);

    public Task<WriteResult> ApplyAsync(string path, MetadataPatch patch, CancellationToken cancellationToken = default) =>
        ApplyAsync(path, patch, null, cancellationToken);

    public async Task<WriteResult> ApplyAsync(string path, MetadataPatch patch, byte[]? frontCover, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patch);
        if (frontCover is { Length: 0 })
        {
            throw new ValidationException("Front cover artwork cannot be empty.");
        }

        var backupPath = await SafeFileCommitService.CommitAsync(
            path,
            (temporaryPath, token) => Task.Run(() => ApplyToFile(temporaryPath, patch, frontCover, token), token),
            (temporaryPath, token) => Task.Run(() =>
            {
                var snapshot = Read(temporaryPath, token);
                VerifyPatch(snapshot.Metadata, patch);
                if (frontCover is not null)
                {
                    VerifyFrontCover(snapshot.FrontCover, frontCover);
                }
            }, token),
            cancellationToken);

        return new(path, backupPath, Read(path, cancellationToken));
    }

    public async Task<WriteResult> ReplaceFrontCoverAsync(string path, byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        if (imageBytes.Length == 0) throw new ValidationException("Choose a non-empty image file.");
        var backupPath = await SafeFileCommitService.CommitAsync(
            path,
            (temporaryPath, token) => Task.Run(() => ReplaceFrontCover(temporaryPath, imageBytes, token), token),
            (temporaryPath, token) => Task.Run(() => VerifyFrontCover(Read(temporaryPath, token).FrontCover, imageBytes), token),
            cancellationToken);
        return new(path, backupPath, Read(path, cancellationToken));
    }
    public async Task<WriteResult> ApplyFullTagsAsync(string path, IReadOnlyDictionary<string, string> tags, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tags);
        var normalizedTags = tags
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(pair => pair.Key.Trim(), pair => pair.Value?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        if (normalizedTags.Count == 0)
        {
            throw new ValidationException("Add at least one tag before saving.");
        }

        var backupPath = await SafeFileCommitService.CommitAsync(
            path,
            (temporaryPath, token) => Task.Run(() => ApplyFullTagsToFile(temporaryPath, normalizedTags, token), token),
            (temporaryPath, token) => Task.Run(() => VerifyFullTags(Read(temporaryPath, token), normalizedTags), token),
            cancellationToken);
        return new(path, backupPath, Read(path, cancellationToken));
    }
    private static AudioFileSnapshot Read(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(path);
        var info = new FileInfo(fullPath);
        if (!info.Exists)
        {
            throw new FileNotFoundException("Audio file was not found.", fullPath);
        }

        var track = new Track(fullPath, true);
        var lyrics = track.Lyrics.FirstOrDefault()?.UnsynchronizedLyrics;
        var metadata = new EditableMetadata(
            track.Title,
            track.Artist,
            track.Album,
            track.AlbumArtist,
            track.Genre,
            track.Composer,
            lyrics,
            track.Copyright,
            ToString(track.Year),
            ToString(track.TrackNumber),
            ToString(track.TrackTotal),
            ToString(track.DiscNumber),
            ToString(track.DiscTotal),
            track.AudioSourceUrl,
            track.ISRC,
            new Dictionary<string, string>(track.AdditionalFields, StringComparer.OrdinalIgnoreCase));
        var properties = new AudioProperties(
            track.AudioFormat?.ToString() ?? "Unknown",
            TimeSpan.FromMilliseconds(track.DurationMs),
            track.Bitrate,
            (int)Math.Round(track.SampleRate),
            0);

        var frontCover = FindCover(track)?.PictureData;
        return new(fullPath, info.Length, info.LastWriteTimeUtc, metadata, properties, frontCover);
    }

    private static void ReplaceFrontCover(string path, byte[] imageBytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var track = new Track(path, true);
        SetFrontCover(track, imageBytes);
        if (!track.Save(null)) throw new IOException("The tag library could not save the cover image.");
    }

    private static void SetFrontCover(Track track, byte[] imageBytes)
    {
        foreach (var picture in track.EmbeddedPictures.Where(IsCoverPicture))
        {
            picture.MarkedForDeletion = true;
        }

        track.EmbeddedPictures.Add(PictureInfo.fromBinaryData(imageBytes, PictureInfo.PIC_TYPE.Front));
    }

    private static PictureInfo? FindCover(Track track) =>
        track.EmbeddedPictures.FirstOrDefault(picture => picture.PicType == PictureInfo.PIC_TYPE.Front)
        ?? track.EmbeddedPictures.FirstOrDefault(picture => picture.PicType == PictureInfo.PIC_TYPE.Generic);

    private static bool IsCoverPicture(PictureInfo picture) =>
        picture.PicType is PictureInfo.PIC_TYPE.Front or PictureInfo.PIC_TYPE.Generic;

    private static void VerifyFrontCover(byte[]? actual, byte[] expected)
    {
        if (actual is null || !actual.SequenceEqual(expected)) throw new IOException("Verification failed for the embedded front cover.");
    }
    private static void ApplyFullTagsToFile(string path, IReadOnlyDictionary<string, string> tags, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var track = new Track(path, true);
        foreach (var (name, value) in tags)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TryMapFullTag(name, out var field))
            {
                ApplyChange(track, string.IsNullOrWhiteSpace(value)
                    ? MetadataChange.Clear(field)
                    : MetadataChange.Set(field, value));
            }
            else
            {
                track.AdditionalFields[name] = value;
            }
        }

        if (!track.Save(null))
        {
            throw new IOException("The tag library could not save the full tag set.");
        }
    }

    private static void VerifyFullTags(AudioFileSnapshot snapshot, IReadOnlyDictionary<string, string> tags)
    {
        foreach (var (name, expected) in tags)
        {
            var actual = TryMapFullTag(name, out var field)
                ? GetValue(snapshot.Metadata, field)
                : snapshot.Metadata.AdditionalFields?.GetValueOrDefault(name);
            if (!string.Equals(actual ?? string.Empty, expected, StringComparison.Ordinal))
            {
                throw new IOException($"Verification failed for {name} after saving full tags.");
            }
        }
    }

    private static bool TryMapFullTag(string name, out MetadataField field)
    {
        var normalized = name.Trim();
        field = normalized.ToUpperInvariant() switch
        {
            "TITLE" => MetadataField.Title,
            "ARTIST" or "TRACK ARTIST" => MetadataField.Artist,
            "ALBUM" => MetadataField.Album,
            "ALBUM ARTIST" => MetadataField.AlbumArtist,
            "GENRE" => MetadataField.Genre,
            "COMPOSER" => MetadataField.Composer,
            "UNSYNCHRONIZED LYRICS" or "LYRICS" => MetadataField.Lyrics,
            "COPYRIGHT" => MetadataField.Copyright,
            "YEAR" => MetadataField.Year,
            "TRACK NUMBER" => MetadataField.TrackNumber,
            "TRACK TOTAL" => MetadataField.TrackTotal,
            "DISC NUMBER" => MetadataField.DiscNumber,
            "DISC TOTAL" => MetadataField.DiscTotal,
            "WEBSITE" => MetadataField.Website,
            "ISRC" => MetadataField.Isrc,
            _ => default
        };
        return normalized.ToUpperInvariant() is "TITLE" or "ARTIST" or "TRACK ARTIST" or "ALBUM" or "ALBUM ARTIST" or "GENRE" or "COMPOSER" or "UNSYNCHRONIZED LYRICS" or "LYRICS" or "COPYRIGHT" or "YEAR" or "TRACK NUMBER" or "TRACK TOTAL" or "DISC NUMBER" or "DISC TOTAL" or "WEBSITE" or "ISRC";
    }
    private static void ApplyToFile(string path, MetadataPatch patch, byte[]? frontCover, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var track = new Track(path, true);
        foreach (var change in patch.Changes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApplyChange(track, change);
        }

        if (frontCover is not null)
        {
            SetFrontCover(track, frontCover);
        }

        if (!track.Save(null))
        {
            throw new IOException("The tag library could not save the staged audio file.");
        }
    }

    private static void ApplyChange(Track track, MetadataChange change)
    {
        var value = change.Intent switch
        {
            FieldIntent.Keep => null,
            FieldIntent.Set when !string.IsNullOrWhiteSpace(change.Value) => change.Value.Trim(),
            FieldIntent.Set => throw new ValidationException($"{change.Field} cannot be empty when set."),
            FieldIntent.Clear => null,
            _ => throw new ArgumentOutOfRangeException(nameof(change))
        };

        if (change.Intent == FieldIntent.Keep)
        {
            return;
        }

        switch (change.Field)
        {
            case MetadataField.Title: track.Title = value; break;
            case MetadataField.Artist: track.Artist = value; break;
            case MetadataField.Album: track.Album = value; break;
            case MetadataField.AlbumArtist: track.AlbumArtist = value; break;
            case MetadataField.Genre: track.Genre = value; break;
            case MetadataField.Composer: track.Composer = value; break;
            case MetadataField.Copyright: track.Copyright = value; break;
            case MetadataField.Website: track.AudioSourceUrl = value; break;
            case MetadataField.Isrc: track.ISRC = value; break;
            case MetadataField.Year: track.Year = ParseInteger(value, change.Field); break;
            case MetadataField.TrackNumber: track.TrackNumber = ParseInteger(value, change.Field); break;
            case MetadataField.TrackTotal: track.TrackTotal = ParseInteger(value, change.Field); break;
            case MetadataField.DiscNumber: track.DiscNumber = ParseInteger(value, change.Field); break;
            case MetadataField.DiscTotal: track.DiscTotal = ParseInteger(value, change.Field); break;
            case MetadataField.Lyrics: ApplyLyrics(track, value); break;
            default: throw new NotSupportedException($"{change.Field} is not implemented by this writer.");
        }
    }

    private static void ApplyLyrics(Track track, string? value)
    {
        track.Lyrics.Clear();
        if (!string.IsNullOrWhiteSpace(value))
        {
            track.Lyrics.Add(new LyricsInfo { UnsynchronizedLyrics = value });
        }
    }

    private static int? ParseInteger(string? value, MetadataField field)
    {
        if (value is null)
        {
            return null;
        }

        if (!int.TryParse(value, out var result) || result < 0)
        {
            throw new ValidationException($"{field} must be a non-negative whole number.");
        }

        return result;
    }

    private static void VerifyPatch(EditableMetadata metadata, MetadataPatch patch)
    {
        foreach (var change in patch.Changes.Where(change => change.Intent != FieldIntent.Keep))
        {
            var expected = change.Intent == FieldIntent.Clear ? null : change.Value?.Trim();
            if (!string.Equals(GetValue(metadata, change.Field), expected, StringComparison.Ordinal))
            {
                throw new IOException($"Verification failed for {change.Field} after staging the write.");
            }
        }
    }

    private static string? GetValue(EditableMetadata metadata, MetadataField field) => field switch
    {
        MetadataField.Title => metadata.Title,
        MetadataField.Artist => metadata.Artist,
        MetadataField.Album => metadata.Album,
        MetadataField.AlbumArtist => metadata.AlbumArtist,
        MetadataField.Genre => metadata.Genre,
        MetadataField.Composer => metadata.Composer,
        MetadataField.Lyrics => metadata.Lyrics,
        MetadataField.Copyright => metadata.Copyright,
        MetadataField.Year => metadata.Year,
        MetadataField.TrackNumber => metadata.TrackNumber,
        MetadataField.TrackTotal => metadata.TrackTotal,
        MetadataField.DiscNumber => metadata.DiscNumber,
        MetadataField.DiscTotal => metadata.DiscTotal,
        MetadataField.Website => metadata.Website,
        MetadataField.Isrc => metadata.Isrc,
        _ => throw new NotSupportedException($"{field} is not implemented by this writer.")
    };

    private static string? ToString<T>(T? value) where T : struct => value?.ToString();
}
