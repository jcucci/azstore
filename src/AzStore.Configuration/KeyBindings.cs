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
    public string Download { get; set; } = "dd";
    
    /// <summary>
    /// Timeout in milliseconds for multi-character key sequences.
    /// If keys are typed slower than this, they are treated as separate commands.
    /// </summary>
    public int KeySequenceTimeout { get; set; } = 1000;
}