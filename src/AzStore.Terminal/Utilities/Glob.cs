using System.Text.RegularExpressions;

namespace AzStore.Terminal.Utilities;

public static class Glob
{
    public static Regex ToRegex(string pattern, bool ignoreCase = true)
    {
        var escaped = Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".");

        var options = RegexOptions.Compiled | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        return new Regex($"^{escaped}$", options);
    }
}

