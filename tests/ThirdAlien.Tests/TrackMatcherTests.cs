using ThirdAlien.Core;

namespace ThirdAlien.Tests;

public sealed class TrackMatcherTests
{
    [Fact]
    public void ProposeAssignsEachCatalogTrackAtMostOnce()
    {
        var files = new[]
        {
            new LocalTrackCandidate("first.mp3", "First", "Artist", 1, 1, TimeSpan.FromMinutes(3)),
            new LocalTrackCandidate("second.mp3", "Second", "Artist", 2, 1, TimeSpan.FromMinutes(4))
        };
        var tracks = new[]
        {
            Track(1, "First", 1, 3),
            Track(2, "Second", 2, 4)
        };

        var matches = TrackMatcher.Propose(files, tracks);

        Assert.Equal(new long?[] { 1, 2 }, matches.Select(match => match.Track?.TrackId));
        Assert.All(matches, match => Assert.False(match.RequiresReview));
    }

    [Fact]
    public void ProposeLeavesAnAmbiguousFileUnmatched()
    {
        var files = new[] { new LocalTrackCandidate("unknown.mp3", null, null, null, null, null) };
        var tracks = new[] { Track(1, "First", 1, 3) };

        var match = Assert.Single(TrackMatcher.Propose(files, tracks));

        Assert.Null(match.Track);
        Assert.True(match.RequiresReview);
    }

    private static CatalogTrack Track(long id, string name, int number, int minutes) => new(
        id, 10, name, "Artist", "Album", "Artist", number, 2, 1, 1,
        new DateOnly(2026, 1, 1), "Rock", null, TimeSpan.FromMinutes(minutes), null, false);
}
