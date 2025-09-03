namespace AzStore.Terminal.Utilities;

public interface IFuzzyMatcher
{
    IEnumerable<FuzzyMatchResult<T>> MatchAndRank<T>(IEnumerable<T> items, Func<T, IEnumerable<string>> keys, string query);
}

