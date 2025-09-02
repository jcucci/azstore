using System.Diagnostics;

namespace AzStore.Terminal.Utilities;

/// <summary>
/// Lightweight, allocation-conscious fuzzy matcher using case-insensitive
/// subsequence/substring scoring. Higher score ranks earlier.
/// </summary>
public class SimpleFuzzyMatcher : IFuzzyMatcher
{
    public IEnumerable<FuzzyMatchResult<T>> MatchAndRank<T>(IEnumerable<T> items, Func<T, IEnumerable<string>> keys, string query)
    {
        query = query?.Trim() ?? string.Empty;
        if (query.Length == 0)
        {
            // No filtering; return neutral score 0
            foreach (var item in items)
                yield return new FuzzyMatchResult<T>(item, 0);
            yield break;
        }

        var q = query.ToLowerInvariant();

        foreach (var item in items)
        {
            var best = int.MinValue;
            foreach (var key in keys(item))
            {
                if (string.IsNullOrEmpty(key))
                    continue;

                var score = Score(key, q);
                if (score > best)
                    best = score;
            }

            if (best > int.MinValue)
                yield return new FuzzyMatchResult<T>(item, best);
        }
    }

    // Simple scoring:
    // - Exact case-insensitive substring: base + 100 - startIndex (earlier is better)
    // - Else subsequence: count of matched chars, contiguous streaks add bonus
    private static int Score(string text, string query)
    {
        var t = text.ToLowerInvariant();

        var idx = t.IndexOf(query, StringComparison.Ordinal);
        if (idx >= 0)
        {
            // Favor earlier match and shorter texts slightly
            return 1000 + 100 - idx - Math.Min(t.Length, 50);
        }

        // subsequence match
        int qi = 0, score = 0, streak = 0;
        for (int ti = 0; ti < t.Length && qi < query.Length; ti++)
        {
            if (t[ti] == query[qi])
            {
                qi++;
                streak++;
                score += 2;            // base per-char
                if (streak > 1) score += 1; // contiguous bonus
            }
            else
            {
                streak = 0;
            }
        }

        return qi == query.Length ? score : int.MinValue; // only if fully matched
    }
}

