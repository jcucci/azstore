using Terminal.Gui;

namespace AzStore.Terminal.UI.Focus;

public class PaneFocusManager
{
    private readonly List<View> _focusChain = [];
    private int _currentIndex = -1;

    public void Register(View view)
    {
        if (_focusChain.Contains(view))
        {
            return;
        }

        _focusChain.Add(view);
    }

    public bool TryGetNext(out View? view)
    {
        view = null;

        if (_focusChain.Count == 0)
        {
            return false;
        }

        var attempts = 0;
        var index = _currentIndex;

        while (attempts < _focusChain.Count)
        {
            index = (index + 1) % _focusChain.Count;
            var candidate = _focusChain[index];

            if (IsFocusable(candidate))
            {
                view = candidate;
                _currentIndex = index;
                return true;
            }

            attempts++;
        }

        return false;
    }

    public bool TryGetPrevious(out View? view)
    {
        view = null;

        if (_focusChain.Count == 0)
        {
            return false;
        }

        var attempts = 0;
        var index = _currentIndex == -1 ? 0 : _currentIndex;

        while (attempts < _focusChain.Count)
        {
            index--;
            if (index < 0)
            {
                index = _focusChain.Count - 1;
            }

            var candidate = _focusChain[index];

            if (IsFocusable(candidate))
            {
                view = candidate;
                _currentIndex = index;
                return true;
            }

            attempts++;
        }

        return false;
    }

    public bool TryGetFirst(out View? view)
    {
        view = null;

        if (_focusChain.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < _focusChain.Count; i++)
        {
            var candidate = _focusChain[i];
            if (IsFocusable(candidate))
            {
                view = candidate;
                _currentIndex = i;
                return true;
            }
        }

        return false;
    }

    public void SetCurrent(View view)
    {
        var index = _focusChain.IndexOf(view);
        if (index >= 0)
        {
            _currentIndex = index;
        }
    }

    private static bool IsFocusable(View view) => view.CanFocus && view.Visible;
}
