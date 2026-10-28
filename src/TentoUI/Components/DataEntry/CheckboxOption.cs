namespace TentoUI;

public class CheckboxOption<TValue>
{
    public TValue Value { get; set; } = default!;
    public string Label { get; set; } = string.Empty;
    public bool Disabled { get; set; }

    public CheckboxOption() { }
    public CheckboxOption(TValue value, string label, bool disabled = false)
    {
        Value = value;
        Label = label;
        Disabled = disabled;
    }
}
