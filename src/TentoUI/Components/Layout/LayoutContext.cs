namespace TentoUI;

public enum SiderTheme
{
    Light,
    Dark
}

public enum Breakpoint
{
    None,
    Xs,
    Sm,
    Md,
    Lg,
    Xl,
    Xxl
}

public class LayoutContext
{
    private int _siderCount = 0;
    public event Action? OnChange;

    public bool HasSider => _siderCount > 0;

    public void RegisterSider()
    {
        _siderCount++;
        OnChange?.Invoke();
    }

    public void UnregisterSider()
    {
        _siderCount = Math.Max(0, _siderCount - 1);
        OnChange?.Invoke();
    }
}
