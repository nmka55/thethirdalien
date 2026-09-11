using ThirdAlien.Core;

namespace ThirdAlien.Tests;

public sealed class WebsiteUrlCleanerTests
{
    [Theory]
    [InlineData("https://example.com", null)]
    [InlineData("www.example.org/path?q=1", null)]
    [InlineData("Visit https://example.com for details", "Visit for details")]
    [InlineData("Cover art from example.co.uk.", "Cover art from")]
    [InlineData("Mail person@example.com", "Mail person@example.com")]
    [InlineData("mix.v1.flac", "mix.v1.flac")]
    [InlineData("Released 2026.09.11", "Released 2026.09.11")]
    public void CleanRemovesOnlyConfidentWebsiteUrls(string input, string? expected)
    {
        var result = WebsiteUrlCleaner.Clean(input);

        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void CleanRemovesMultipleUrlsWithoutVisitingThem()
    {
        var result = WebsiteUrlCleaner.Clean("See https://example.com and www.example.org now");

        Assert.Equal("See and now", result.Value);
        Assert.Equal(2, result.RemovedCount);
    }
}
