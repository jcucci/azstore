using AzStore.Terminal.Theming;
using Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public class SearchPaneView : PaneViewBase
{
    private readonly Label _placeholder;

    public SearchPaneView(IThemeService theme) : base("Search", theme)
    {
        _placeholder = CreatePlaceholder("Search prefix");
        Add(_placeholder);
    }

    public Label Placeholder => _placeholder;
}
