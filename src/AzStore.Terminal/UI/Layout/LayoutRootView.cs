using AzStore.Terminal.Theming;
using AzStore.Terminal.UI.Focus;
using AzStore.Terminal.UI.Panes;
using Terminal.Gui;
using Tui = global::Terminal.Gui;

namespace AzStore.Terminal.UI.Layout;

public class LayoutRootView : View
{
    private const int HeaderHeight = 5;
    private const int FooterHeight = 7;
    private const int CommandEditorHeight = 3;

    private readonly PaneFocusManager _focusManager = new();

    public SessionPaneView SessionPane { get; }
    public StorageAccountPaneView StorageAccountPane { get; }
    public ContainerPaneView ContainerPane { get; }
    public SearchPaneView SearchPane { get; }
    public ResultsPaneView ResultsPane { get; }
    public PreviewPaneView PreviewPane { get; }
    public CommandEditorPaneView CommandEditorPane { get; }
    public CommandHistoryPaneView CommandHistoryPane { get; }
    public DownloadsPaneView DownloadsPane { get; }

    public LayoutRootView(BlobBrowserView browserView, IThemeService theme)
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        ColorScheme = new ColorScheme
        {
            Normal = theme.ResolveTui(ThemeToken.Title),
            Focus = theme.ResolveTui(ThemeToken.Selection),
            HotNormal = theme.ResolveTui(ThemeToken.Title),
            HotFocus = theme.ResolveTui(ThemeToken.Selection),
            Disabled = new Tui.Attribute(Color.DarkGray, theme.ResolveBackground())
        };

        var header = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = HeaderHeight
        };

        SessionPane = new SessionPaneView(theme);
        var sessionPaneChrome = CreateChrome(SessionPane, theme,
            x: 0,
            width: Dim.Percent(25));

        StorageAccountPane = new StorageAccountPaneView(theme);
        var storageAccountPaneChrome = CreateChrome(StorageAccountPane, theme,
            x: Pos.Right(sessionPaneChrome),
            width: Dim.Percent(25));

        ContainerPane = new ContainerPaneView(theme);
        var containerPaneChrome = CreateChrome(ContainerPane, theme,
            x: Pos.Right(storageAccountPaneChrome),
            width: Dim.Percent(25));

        SearchPane = new SearchPaneView(theme);
        var searchPaneChrome = CreateChrome(SearchPane, theme,
            x: Pos.Right(containerPaneChrome),
            width: Dim.Fill());

        header.Add(sessionPaneChrome, storageAccountPaneChrome, containerPaneChrome, searchPaneChrome);

        var body = new View
        {
            X = 0,
            Y = Pos.Bottom(header),
            Width = Dim.Fill(),
            Height = Dim.Fill(FooterHeight)
        };

        ResultsPane = new ResultsPaneView(browserView, theme);
        var resultsPaneChrome = CreateChrome(ResultsPane, theme,
            x: 0,
            width: Dim.Percent(60));

        PreviewPane = new PreviewPaneView(theme);
        var previewPaneChrome = CreateChrome(PreviewPane, theme,
            x: Pos.Right(resultsPaneChrome),
            width: Dim.Fill());

        body.Add(resultsPaneChrome, previewPaneChrome);

        var footer = new View
        {
            X = 0,
            Y = Pos.Bottom(body),
            Width = Dim.Fill(),
            Height = FooterHeight
        };

        var footerLeft = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(70),
            Height = Dim.Fill()
        };

        CommandEditorPane = new CommandEditorPaneView(theme);
        var commandEditorPaneChrome = CreateChrome(CommandEditorPane, theme,
            x: 0,
            width: Dim.Fill(),
            height: CommandEditorHeight);

        CommandHistoryPane = new CommandHistoryPaneView(theme);
        var commandHistoryPaneChrome = CreateChrome(CommandHistoryPane, theme,
            x: 0,
            y: Pos.Bottom(commandEditorPaneChrome),
            width: Dim.Fill(),
            height: Dim.Fill());

        footerLeft.Add(commandEditorPaneChrome, commandHistoryPaneChrome);

        DownloadsPane = new DownloadsPaneView(theme);
        var downloadsPaneChrome = CreateChrome(DownloadsPane, theme,
            x: Pos.Right(footerLeft),
            width: Dim.Fill(),
            height: Dim.Fill());

        footer.Add(footerLeft, downloadsPaneChrome);

        Add(header, body, footer);

        RegisterFocusTargets();
    }

    protected override bool OnKeyDown(Key keyEvent)
    {
        if (HandleFocusTraversal(keyEvent))
        {
            return true;
        }

        return base.OnKeyDown(keyEvent);
    }

    private void RegisterFocusTargets()
    {
        _focusManager.Register(SessionPane);
        _focusManager.Register(StorageAccountPane);
        _focusManager.Register(ContainerPane);
        _focusManager.Register(SearchPane);
        _focusManager.Register(ResultsPane);
        _focusManager.Register(PreviewPane);
        _focusManager.Register(DownloadsPane);
        _focusManager.Register(CommandEditorPane);
        _focusManager.Register(CommandHistoryPane);
    }

    internal bool HandleFocusTraversal(Key keyEvent)
    {
        if (IsForwardTraversalKey(keyEvent))
        {
            if (_focusManager.TryGetNext(out var next) && next != null)
            {
                next.SetFocus();
                return true;
            }
        }

        if (IsBackwardTraversalKey(keyEvent))
        {
            if (_focusManager.TryGetPrevious(out var previous) && previous != null)
            {
                previous.SetFocus();
                return true;
            }
        }

        return false;
    }

    public void ScheduleInitialFocus()
    {
        Application.AddIdle(() =>
        {
            SearchPane.SetFocus();
            _focusManager.SetCurrent(SearchPane);
            return false;
        });
    }

    private static PaneChromeView CreateChrome(
        PaneViewBase pane,
        IThemeService theme,
        Pos? x = null,
        Pos? y = null,
        Dim? width = null,
        Dim? height = null)
    {
        var chrome = new PaneChromeView(pane, theme)
        {
            X = x ?? 0,
            Y = y ?? 0,
            Width = width ?? Dim.Fill(),
            Height = height ?? Dim.Fill()
        };

        return chrome;
    }

    internal static bool IsTraversalKey(Key key) => IsForwardTraversalKey(key) || IsBackwardTraversalKey(key);

    private static bool IsForwardTraversalKey(Key key) => key == Application.NextTabKey || key == Key.Tab;

    private static bool IsBackwardTraversalKey(Key key) => key == Application.PrevTabKey || key == Key.Tab.WithShift;
}
