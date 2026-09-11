using ThirdAlien.Infrastructure;

namespace ThirdAlien.Tests;

public sealed class SafeFileCommitServiceTests
{
    [Fact]
    public async Task CommitAsyncReplacesOriginalOnlyAfterVerificationAndKeepsBackup()
    {
        var directory = Path.Combine(Path.GetTempPath(), "thirdalien-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var source = Path.Combine(directory, "song.mp3");
        await File.WriteAllTextAsync(source, "before");

        try
        {
            var backup = await SafeFileCommitService.CommitAsync(
                source,
                (temporary, cancellationToken) => File.WriteAllTextAsync(temporary, "after", cancellationToken),
                async (temporary, cancellationToken) => Assert.Equal("after", await File.ReadAllTextAsync(temporary, cancellationToken)));

            Assert.Equal("after", await File.ReadAllTextAsync(source));
            Assert.Equal("before", await File.ReadAllTextAsync(backup));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
