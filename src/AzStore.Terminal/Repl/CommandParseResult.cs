namespace AzStore.Terminal.Repl;

public sealed record CommandParseResult(string CommandName, string[] Arguments, bool IsForce);

