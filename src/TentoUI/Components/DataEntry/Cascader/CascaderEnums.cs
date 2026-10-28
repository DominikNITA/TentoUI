namespace TentoUI;

public class CascaderOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Disabled { get; set; }
    public System.Collections.Generic.List<CascaderOption> Children { get; set; } = new();

    public CascaderOption() { }

    public CascaderOption(string value, string label, IEnumerable<CascaderOption>? children = null, bool disabled = false)
    {
        Value = value;
        Label = label;
        Disabled = disabled;
        if (children != null)
        {
            Children.AddRange(children);
        }
    }
}
