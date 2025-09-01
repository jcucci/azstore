namespace AzStore.Configuration;

public class KeyBindings
{
    public string MoveDown { get; set; } = "j";
    public string MoveUp { get; set; } = "k";
    public string Enter { get; set; } = "l";
    public string Back { get; set; } = "h";
    public string Search { get; set; } = "/";
    public string Command { get; set; } = ":";
    public string Top { get; set; } = "gg";
    public string Bottom { get; set; } = "G";
    public string Download { get; set; } = "d";
    public string Refresh { get; set; } = "r";
    public string Info { get; set; } = "i";
    public string Help { get; set; } = "?";

    /// <summary>
    /// Timeout in milliseconds for multi-character key sequences.
    /// If keys are typed slower than this, they are treated as separate commands.
    /// </summary>
    public int KeySequenceTimeout { get; set; } = 1000;

    /// <summary>
    /// Initial delay in milliseconds before key repeat starts when a key is held down.
    /// </summary>
    public int KeyRepeatDelay { get; set; } = 500;

    /// <summary>
    /// Interval in milliseconds between key repeat events once repeat has started.
    /// </summary>
    public int KeyRepeatInterval { get; set; } = 50;
}