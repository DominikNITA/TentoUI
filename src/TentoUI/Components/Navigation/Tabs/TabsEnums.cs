using Microsoft.AspNetCore.Components;

namespace TentoUI;

public enum TabType
{
    Line,
    Card,
    EditableCard
}

public enum TabPosition
{
    Top,
    Bottom,
    Left,
    Right
}

public class TabEditEventArgs
{
    public string Action { get; set; } = string.Empty; // "add" or "remove"
    public string? Key { get; set; }
}

public class TabPaneInfo
{
    public string Key { get; set; } = string.Empty;
    public string? Tab { get; set; }
    public RenderFragment? TabTemplate { get; set; }
    public RenderFragment? Icon { get; set; }
    public bool Disabled { get; set; }
    public bool Closable { get; set; } = true;
    public RenderFragment? Content { get; set; }
}

public class TabsContext
{
    private readonly System.Collections.Generic.List<TabPaneInfo> _panes = new();

    public event Action? OnStateChange;
    public IReadOnlyList<TabPaneInfo> Panes => _panes.AsReadOnly();
    public string? ActiveKey { get; set; }

    public void RegisterPane(TabPaneInfo pane)
    {
        var existing = _panes.FirstOrDefault(p => p.Key == pane.Key);
        if (existing != null)
        {
            _panes.Remove(existing);
        }
        _panes.Add(pane);

        if (string.IsNullOrEmpty(ActiveKey) && !pane.Disabled)
        {
            ActiveKey = pane.Key;
        }

        OnStateChange?.Invoke();
    }

    public void NotifyPaneUpdated()
    {
        OnStateChange?.Invoke();
    }

    public void UnregisterPane(string key)
    {
        _panes.RemoveAll(p => p.Key == key);
        if (ActiveKey == key && _panes.Any())
        {
            ActiveKey = _panes.FirstOrDefault(p => !p.Disabled)?.Key;
        }
        OnStateChange?.Invoke();
    }

    public void SetActiveKey(string key)
    {
        ActiveKey = key;
        OnStateChange?.Invoke();
    }
}
