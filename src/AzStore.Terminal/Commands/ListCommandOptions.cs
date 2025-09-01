namespace AzStore.Terminal.Commands;

public sealed record ListCommandOptions(string? Container, string? Pattern, string SortKey, bool Descending)
{
    public static ListCommandOptions FromArgs(string[] args)
    {
        string? containerFilter = null;
        string? pattern = null;
        var sortKey = "name";
        var descending = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.IsNullOrWhiteSpace(arg)) continue;

            if (arg.StartsWith("--sort=", StringComparison.OrdinalIgnoreCase))
            {
                var eq = arg.IndexOf('=');
                if (eq >= 0 && eq < arg.Length - 1)
                {
                    sortKey = arg[(eq + 1)..].Trim();
                }
                continue;
            }

            if (string.Equals(arg, "--sort", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    sortKey = args[i + 1];
                    i++;
                }
                continue;
            }

            if (string.Equals(arg, "--desc", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--reverse", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-r", StringComparison.Ordinal))
            {
                descending = true;
                continue;
            }

            if (arg.Contains('*') || arg.Contains('?'))
            {
                pattern ??= arg.Trim();
            }
            else if (!arg.StartsWith('-'))
            {
                if (containerFilter is not null)
                {
                    pattern ??= arg.Trim();
                }
                else
                {
                    containerFilter = arg.Trim();
                }
            }
        }

        return new ListCommandOptions(containerFilter, pattern, sortKey, descending);
    }
}
