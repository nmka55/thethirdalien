using System.Globalization;
using System.Text;

namespace ThirdAlien.Core;

public sealed record LocalTrackCandidate(string FilePath, string? Title, string? Artist, int? TrackNumber, int? DiscNumber, TimeSpan? Duration);

public sealed record TrackMatch(LocalTrackCandidate File, CatalogTrack? Track, int Score, string Reason)
{
    public bool RequiresReview => Track is null || Score < 80;
}

public static class TrackMatcher
{
    public static IReadOnlyList<TrackMatch> Propose(
        IReadOnlyList<LocalTrackCandidate> files,
        IReadOnlyList<CatalogTrack> tracks)
    {
        var available = tracks.ToList();
        var matches = new List<TrackMatch>(files.Count);

        foreach (var file in files)
        {
            var ranked = available
                .Select(track => (Track: track, Score: Score(file, track)))
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Track.DiscNumber)
                .ThenBy(candidate => candidate.Track.TrackNumber)
                .FirstOrDefault();

            if (ranked.Track is null || ranked.Score < 45)
            {
                matches.Add(new(file, null, ranked.Score, "No safe automatic match"));
                continue;
            }

            available.Remove(ranked.Track);
            matches.Add(new(file, ranked.Track, ranked.Score, DescribeMatch(file, ranked.Track, ranked.Score)));
        }

        return matches;
    }

    private static int Score(LocalTrackCandidate file, CatalogTrack track)
    {
        var score = 0;
        if (file.TrackNumber is not null && file.TrackNumber == track.TrackNumber)
        {
            score += 55;
        }

        if (file.DiscNumber is not null && file.DiscNumber == track.DiscNumber)
        {
            score += 15;
        }

        if (SameNormalizedText(file.Title, track.Name))
        {
            score += 45;
        }

        if (SameNormalizedText(file.Artist, track.Artist))
        {
            score += 20;
        }

        if (file.Duration is not null && track.Duration is not null)
        {
            var difference = Math.Abs((file.Duration.Value - track.Duration.Value).TotalSeconds);
            score += difference switch
            {
                <= 2 => 15,
                <= 5 => 8,
                _ => 0
            };
        }

        return score;
    }

    private static string DescribeMatch(LocalTrackCandidate file, CatalogTrack track, int score) =>
        score >= 100 ? "Track number and metadata agree" : $"Proposed match to {track.Name}";

    private static bool SameNormalizedText(string? first, string? second) =>
        !string.IsNullOrWhiteSpace(first)
        && !string.IsNullOrWhiteSpace(second)
        && string.Equals(Normalize(first), Normalize(second), StringComparison.Ordinal);

    private static string Normalize(string input)
    {
        var builder = new StringBuilder(input.Length);
        foreach (var character in input.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }
}
