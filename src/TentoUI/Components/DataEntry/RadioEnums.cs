namespace TentoUI;

public enum RadioOptionType
{
    Default,
    Button
}

public enum RadioButtonStyle
{
    Outline,
    Solid
}

public class RadioOptionItem<TValue>
{
    public TValue Value { get; set; } = default!;
    public string Label { get; set; } = string.Empty;
    public bool Disabled { get; set; }
}
