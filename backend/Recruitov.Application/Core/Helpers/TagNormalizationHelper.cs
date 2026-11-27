namespace Recruitov.Application.Core.Helpers;

public record NormalizedTag(string DisplayName, string NormalizedName);

public static class TagNormalizationHelper
{
    public static IReadOnlyList<NormalizedTag> NormalizeTags(IEnumerable<string>? rawTags)
    {
        if (rawTags is null)
            return [];

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<NormalizedTag>();

        foreach (var raw in rawTags)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var trimmed = raw.Trim();
            var normalized = trimmed.ToLowerInvariant();

            if (seen.Add(normalized))
                result.Add(new NormalizedTag(trimmed, normalized));
        }

        return result;
    }

    public static string NormalizeSingle(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
}
