namespace ThirdAlien.Infrastructure;

public static class SafeFileCommitService
{
    public static async Task<string> CommitAsync(
        string path,
        Func<string, CancellationToken, Task> writeTemporaryCopy,
        Func<string, CancellationToken, Task> verifyTemporaryCopy,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(writeTemporaryCopy);
        ArgumentNullException.ThrowIfNull(verifyTemporaryCopy);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new IOException("The file has no parent directory.");
        var extension = Path.GetExtension(fullPath);
        var stem = Path.GetFileNameWithoutExtension(fullPath);
        var token = Guid.NewGuid().ToString("N");
        var temporaryPath = Path.Combine(directory, $"{stem}.thirdalien-temp-{token}{extension}");
        var backupPath = Path.Combine(directory, $"{stem}.thirdalien-backup-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{token}{extension}");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Copy(fullPath, temporaryPath, overwrite: false);
            await writeTemporaryCopy(temporaryPath, cancellationToken);
            await verifyTemporaryCopy(temporaryPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Replace(temporaryPath, fullPath, backupPath, ignoreMetadataErrors: false);
            return backupPath;
        }
        catch
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            throw;
        }
    }
}
