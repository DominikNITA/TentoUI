namespace TentoUI;

public enum SelectMode
{
    Single,
    Multiple
}

public class SelectOptionItem<TValue>
{
    public TValue Value { get; set; } = default!;
    public string Label { get; set; } = string.Empty;
    public bool Disabled { get; set; }
}
