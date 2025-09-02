namespace AzStore.Configuration;

public class TerminalSelectionOptions
{
    public bool EnableFuzzySearch { get; set; } = true;
    public int MaxVisibleItems { get; set; } = 15; // default within 12–20 range
    public bool HighlightMatches { get; set; } = true;
    public int? PickerTimeoutMs { get; set; } = null;
}

