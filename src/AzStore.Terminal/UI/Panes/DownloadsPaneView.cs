using AzStore.Terminal.Theming;
using Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public class DownloadsPaneView : PaneViewBase
{
    private readonly Label _placeholder;

    public DownloadsPaneView(IThemeService theme) : base("Downloads", theme)
    {
        _placeholder = CreatePlaceholder("No downloads in progress");
        Add(_placeholder);
    }

    public Label Placeholder => _placeholder;
}
