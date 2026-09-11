namespace ThirdAlien.Core;

public interface IAudioTagService
{
    Task<AudioFileSnapshot> ReadAsync(string path, CancellationToken cancellationToken = default);

    Task<WriteResult> ApplyAsync(string path, MetadataPatch patch, CancellationToken cancellationToken = default);

    Task<WriteResult> ApplyAsync(string path, MetadataPatch patch, byte[]? frontCover, CancellationToken cancellationToken = default);

    Task<WriteResult> ReplaceFrontCoverAsync(string path, byte[] imageBytes, CancellationToken cancellationToken = default);

    Task<WriteResult> ApplyFullTagsAsync(string path, IReadOnlyDictionary<string, string> tags, CancellationToken cancellationToken = default);
}

public sealed record WriteResult(string Path, string BackupPath, AudioFileSnapshot Snapshot);