namespace AzStore.Terminal.Utilities;

public readonly record struct FuzzyMatchResult<T>(T Item, int Score);

