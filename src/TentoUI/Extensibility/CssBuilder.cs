namespace TentoUI.Extensibility;

public class CssBuilder
{
    private readonly System.Text.StringBuilder _sb = new();

    public CssBuilder() { }

    public CssBuilder(string? initialClass)
    {
        if (!string.IsNullOrWhiteSpace(initialClass))
            _sb.Append(initialClass.Trim());
    }

    public CssBuilder AddClass(string? value, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(value))
        {
            if (_sb.Length > 0)
                _sb.Append(' ');
            _sb.Append(value.Trim());
        }
        return this;
    }

    public CssBuilder AddClass(string? value, Func<bool>? condition) =>
        AddClass(value, condition?.Invoke() ?? false);

    public CssBuilder AddClassFromAttributes(IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes != null && attributes.TryGetValue("class", out var classVal) && classVal != null)
        {
            AddClass(classVal.ToString());
        }
        return this;
    }

    public string Build() => _sb.ToString();
    public override string ToString() => Build();
}
