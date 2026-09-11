using System.Globalization;
using System.Text.RegularExpressions;

namespace ThirdAlien.Core;

public sealed record UrlCleanupResult(string? Value, int RemovedCount)
{
    public bool Changed => RemovedCount > 0;
}

public static partial class WebsiteUrlCleaner
{
    private static readonly HashSet<string> RecognizedTopLevelDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "app", "biz", "co", "com", "dev", "edu", "gov", "info", "io", "me", "mobi", "net", "org", "tv", "uk", "us"
    };

    [GeneratedRegex(@"(?<![\w@])(?<url>(?:https?://|www\.)[^\s<>\[\]{}]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SchemeOrWwwRegex();

    [GeneratedRegex(@"(?<![\w@])(?<url>[\p{L}\p{N}](?:[\p{L}\p{N}-]{0,61}[\p{L}\p{N}])?(?:\.[\p{L}\p{N}](?:[\p{L}\p{N}-]{0,61}[\p{L}\p{N}]?))+)(?:/[^\s<>\[\]{}]*)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BareDomainRegex();

    public static UrlCleanupResult Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new(value, 0);
        }

        var removed = 0;
        var cleaned = SchemeOrWwwRegex().Replace(value, match =>
        {
            var candidate = TrimTrailingPunctuation(match.Groups["url"].Value);
            if (!IsWebsite(candidate))
            {
                return match.Value;
            }

            removed++;
            return PreserveNonUrlSuffix(match.Value[candidate.Length..]);
        });

        cleaned = BareDomainRegex().Replace(cleaned, match =>
        {
            var candidate = TrimTrailingPunctuation(match.Groups["url"].Value);
            if (!IsBareWebsite(candidate))
            {
                return match.Value;
            }

            removed++;
            return PreserveNonUrlSuffix(match.Value[candidate.Length..]);
        });

        cleaned = NormalizeWhitespace(cleaned);
        return new(string.IsNullOrWhiteSpace(cleaned) ? null : cleaned, removed);
    }


    private static string PreserveNonUrlSuffix(string suffix) =>
        suffix.Length == 1 && suffix[0] is '.' or ',' or ';' or ':' or '!' or '?'
            ? string.Empty
            : suffix;

    private static bool IsWebsite(string candidate)
    {
        var normalized = candidate.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? $"https://{candidate}"
            : candidate;

        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(uri.Host)
            && uri.Host.Contains('.', StringComparison.Ordinal);
    }

    private static bool IsBareWebsite(string candidate)
    {
        var host = candidate.Split('/', 2)[0];
        var labels = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (labels.Length < 2 || labels.Any(label => label.Length == 0))
        {
            return false;
        }

        var lastLabel = labels[^1];
        var asciiTopLevelDomain = new IdnMapping().GetAscii(lastLabel);
        return RecognizedTopLevelDomains.Contains(asciiTopLevelDomain);
    }

    private static string TrimTrailingPunctuation(string value) => value.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '}');

    private static string NormalizeWhitespace(string value)
    {
        var normalized = Regex.Replace(value, @"[ \t]{2,}", " ").Trim();
        return Regex.Replace(normalized, @"\s+[.,;:!?]$", string.Empty);
    }
}
