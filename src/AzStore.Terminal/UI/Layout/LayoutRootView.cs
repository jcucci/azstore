using AzStore.Terminal.Theming;
using AzStore.Terminal.UI.Focus;
using AzStore.Terminal.UI.Panes;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using Tui = global::Terminal.Gui;

namespace AzStore.Terminal.UI.Layout;

public class LayoutRootView : View
{
    private const int HeaderHeight = 5;
    private const int FooterHeight = 7;
    private const int CommandEditorHeight = 3;

    private readonly PaneFocusManager _focusManager = new();
    private readonly ILogger<PaneChromeView>? _chromeLogger;
    private readonly ILogger<LayoutRootView>? _logger;
    private readonly List<PaneChromeView> _allChromes = new();

    public SessionPaneView SessionPane { get; }
    public StorageAccountPaneView StorageAccountPane { get; }
    public ContainerPaneView ContainerPane { get; }
    public SearchPaneView SearchPane { get; }
    public ResultsPaneView ResultsPane { get; }
    public PreviewPaneView PreviewPane { get; }
    public CommandEditorPaneView CommandEditorPane { get; }
    public CommandHistoryPaneView CommandHistoryPane { get; }
    public DownloadsPaneView DownloadsPane { get; }

    private PaneChromeView? _sessionChrome;
    private PaneChromeView? _storageAccountChrome;
    private PaneChromeView? _containerChrome;
    private PaneChromeView? _searchChrome;
    private PaneChromeView? _resultsChrome;
    private PaneChromeView? _previewChrome;
    private PaneChromeView? _commandEditorChrome;
    private PaneChromeView? _commandHistoryChrome;
    private PaneChromeView? _downloadsChrome;

    public LayoutRootView(BlobBrowserView browserView, IThemeService theme, ILogger<PaneChromeView>? chromeLogger = null, ILogger<LayoutRootView>? logger = null)
    {
        _chromeLogger = chromeLogger;
        _logger = logger;
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
        _sessionChrome = CreateChrome(SessionPane, theme,
            x: 0,
            width: Dim.Percent(25));

        StorageAccountPane = new StorageAccountPaneView(theme);
        _storageAccountChrome = CreateChrome(StorageAccountPane, theme,
            x: Pos.Right(_sessionChrome),
            width: Dim.Percent(25));

        ContainerPane = new ContainerPaneView(theme);
        _containerChrome = CreateChrome(ContainerPane, theme,
            x: Pos.Right(_storageAccountChrome),
            width: Dim.Percent(25));

        SearchPane = new SearchPaneView(theme);
        _searchChrome = CreateChrome(SearchPane, theme,
            x: Pos.Right(_containerChrome),
            width: Dim.Fill());

        header.Add(_sessionChrome, _storageAccountChrome, _containerChrome, _searchChrome);

        var body = new View
        {
            X = 0,
            Y = Pos.Bottom(header),
            Width = Dim.Fill(),
            Height = Dim.Fill(FooterHeight)
        };

        ResultsPane = new ResultsPaneView(browserView, theme);
        _resultsChrome = CreateChrome(ResultsPane, theme,
            x: 0,
            width: Dim.Percent(60));

        PreviewPane = new PreviewPaneView(theme);
        _previewChrome = CreateChrome(PreviewPane, theme,
            x: Pos.Right(_resultsChrome),
            width: Dim.Fill());

        body.Add(_resultsChrome, _previewChrome);

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
        _commandEditorChrome = CreateChrome(CommandEditorPane, theme,
            x: 0,
            width: Dim.Fill(),
            height: CommandEditorHeight);

        CommandHistoryPane = new CommandHistoryPaneView(theme);
        _commandHistoryChrome = CreateChrome(CommandHistoryPane, theme,
            x: 0,
            y: Pos.Bottom(_commandEditorChrome),
            width: Dim.Fill(),
            height: Dim.Fill());

        footerLeft.Add(_commandEditorChrome, _commandHistoryChrome);

        DownloadsPane = new DownloadsPaneView(theme);
        _downloadsChrome = CreateChrome(DownloadsPane, theme,
            x: Pos.Right(footerLeft),
            width: Dim.Fill(),
            height: Dim.Fill());

        footer.Add(footerLeft, _downloadsChrome);

        Add(header, body, footer);

        RegisterFocusTargets();
    }

    protected override bool OnKeyDown(Key keyEvent)
    {
        _logger?.LogDebug("OnKeyDown: Key={Key}", keyEvent);
        return base.OnKeyDown(keyEvent);
    }

    private void RegisterFocusTargets()
    {
        // Register chrome views and track them all
        if (_sessionChrome != null) { _focusManager.Register(_sessionChrome); _allChromes.Add(_sessionChrome); }
        if (_storageAccountChrome != null) { _focusManager.Register(_storageAccountChrome); _allChromes.Add(_storageAccountChrome); }
        if (_containerChrome != null) { _focusManager.Register(_containerChrome); _allChromes.Add(_containerChrome); }
        if (_searchChrome != null) { _focusManager.Register(_searchChrome); _allChromes.Add(_searchChrome); }
        if (_resultsChrome != null) { _focusManager.Register(_resultsChrome); _allChromes.Add(_resultsChrome); }
        if (_previewChrome != null) { _focusManager.Register(_previewChrome); _allChromes.Add(_previewChrome); }
        if (_downloadsChrome != null) { _focusManager.Register(_downloadsChrome); _allChromes.Add(_downloadsChrome); }
        if (_commandEditorChrome != null) { _focusManager.Register(_commandEditorChrome); _allChromes.Add(_commandEditorChrome); }
        if (_commandHistoryChrome != null) { _focusManager.Register(_commandHistoryChrome); _allChromes.Add(_commandHistoryChrome); }
    }

    private void SetActiveChrome(PaneChromeView activeChrome)
    {
        _logger?.LogDebug("Setting active chrome: {Title}", activeChrome.Title);

        // Update all chromes: active one gets focus scheme, others get normal scheme
        foreach (var chrome in _allChromes)
        {
            chrome.SetActiveState(chrome == activeChrome);
        }
    }

    internal bool HandleFocusTraversal(Key keyEvent)
    {
        if (IsForwardTraversalKey(keyEvent))
        {
            _logger?.LogDebug("Forward traversal key detected");
            if (_focusManager.TryGetNext(out var next) && next != null && next is PaneChromeView chrome)
            {
                SetActiveChrome(chrome);
                return true;
            }
            _logger?.LogDebug("No next view available");
        }

        if (IsBackwardTraversalKey(keyEvent))
        {
            _logger?.LogDebug("Backward traversal key detected");
            if (_focusManager.TryGetPrevious(out var previous) && previous != null && previous is PaneChromeView chrome)
            {
                SetActiveChrome(chrome);
                return true;
            }
            _logger?.LogDebug("No previous view available");
        }

        return false;
    }

    public void ScheduleInitialFocus()
    {
        Application.AddIdle(() =>
        {
            if (_searchChrome != null)
            {
                _searchChrome.SetFocus();
                _focusManager.SetCurrent(_searchChrome);
            }
            return false;
        });
    }

    private PaneChromeView CreateChrome(
        PaneViewBase pane,
        IThemeService theme,
        Pos? x = null,
        Pos? y = null,
        Dim? width = null,
        Dim? height = null)
    {
        var chrome = new PaneChromeView(pane, theme, _chromeLogger)
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
