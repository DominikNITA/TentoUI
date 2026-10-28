namespace TentoUI;

public enum MenuMode
{
    Horizontal,
    Vertical,
    Inline
}

public enum MenuTheme
{
    Light,
    Dark
}

public class MenuSelectEventArgs
{
    public string Key { get; set; } = string.Empty;
    public object? Item { get; set; }
}

public class MenuContext
{
    public MenuMode Mode { get; set; } = MenuMode.Inline;
    public MenuTheme Theme { get; set; } = MenuTheme.Light;
    public string? SelectedKey { get; set; }
    public System.Collections.Generic.List<string> OpenKeys { get; set; } = new();
    public bool Collapsed { get; set; }

    private readonly System.Collections.Generic.List<(string Key, bool Disabled)> _registeredItems = new();
    public IReadOnlyList<(string Key, bool Disabled)> RegisteredItems => _registeredItems;

    public event Action? OnStateChange;

    public void RegisterItem(string key, bool disabled)
    {
        if (string.IsNullOrEmpty(key)) return;
        var idx = _registeredItems.FindIndex(i => i.Key == key);
        if (idx >= 0)
        {
            _registeredItems[idx] = (key, disabled);
        }
        else
        {
            _registeredItems.Add((key, disabled));
        }
    }

    public void UpdateItem(string key, bool disabled)
    {
        if (string.IsNullOrEmpty(key)) return;
        var idx = _registeredItems.FindIndex(i => i.Key == key);
        if (idx >= 0)
        {
            _registeredItems[idx] = (key, disabled);
        }
        else
        {
            _registeredItems.Add((key, disabled));
        }
    }

    public void UnregisterItem(string key)
    {
        _registeredItems.RemoveAll(i => i.Key == key);
    }

    public void SelectKey(string key)
    {
        SelectedKey = key;
        OnStateChange?.Invoke();
    }

    public void SelectNext(string currentKey)
    {
        var enabled = _registeredItems.Where(i => !i.Disabled).Select(i => i.Key).ToList();
        if (enabled.Count == 0) return;
        var idx = enabled.IndexOf(currentKey);
        var nextIdx = (idx + 1) % enabled.Count;
        SelectKey(enabled[nextIdx]);
    }

    public void SelectPrevious(string currentKey)
    {
        var enabled = _registeredItems.Where(i => !i.Disabled).Select(i => i.Key).ToList();
        if (enabled.Count == 0) return;
        var idx = enabled.IndexOf(currentKey);
        var prevIdx = (idx - 1 + enabled.Count) % enabled.Count;
        SelectKey(enabled[prevIdx]);
    }

    public void ToggleOpenKey(string key)
    {
        if (OpenKeys.Contains(key))
            OpenKeys.Remove(key);
        else
            OpenKeys.Add(key);
        OnStateChange?.Invoke();
    }

    public bool IsSelected(string key) => SelectedKey == key;
    public bool IsOpen(string key) => OpenKeys.Contains(key);
}
