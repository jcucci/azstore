namespace AzStore.Terminal.Repl;

public static class CommandParser
{
    public static CommandParseResult Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input[0] != ':')
            return new CommandParseResult(string.Empty, Array.Empty<string>(), false);

        var trimmed = input.Trim();
        var withoutColon = trimmed.TrimStart(':');

        var firstSpace = withoutColon.IndexOf(' ');
        var commandToken = firstSpace >= 0 ? withoutColon[..firstSpace] : withoutColon;
        var rest = firstSpace >= 0 ? withoutColon[(firstSpace + 1)..] : string.Empty;

        var isForce = commandToken.EndsWith('!');
        var commandName = isForce ? commandToken[..^1] : commandToken;

        var args = SplitArgs(rest);
        return new CommandParseResult(commandName, args, isForce);
    }

    private static string[] SplitArgs(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Array.Empty<string>();

        return input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

