using AzStore.Terminal.Theming;
using AzStore.Terminal.UI;
using Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public class ResultsPaneView : PaneViewBase
{
    public BlobBrowserView BrowserView { get; }

    public ResultsPaneView(BlobBrowserView browserView, IThemeService theme) : base("Results", theme)
    {
        BrowserView = browserView;
        BrowserView.X = 0;
        BrowserView.Y = 0;
        BrowserView.Width = Dim.Fill();
        BrowserView.Height = Dim.Fill();
        BrowserView.CanFocus = true;

        Add(BrowserView);
    }
}
