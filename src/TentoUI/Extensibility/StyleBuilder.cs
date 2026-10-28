namespace TentoUI.Extensibility;

public class StyleBuilder
{
    private readonly System.Text.StringBuilder _sb = new();

    public StyleBuilder() { }

    public StyleBuilder(string? initialStyle)
    {
        if (!string.IsNullOrWhiteSpace(initialStyle))
        {
            _sb.Append(initialStyle.Trim());
            if (!initialStyle.TrimEnd().EndsWith(';'))
                _sb.Append(';');
        }
    }

    public StyleBuilder AddStyle(string? style, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(style))
        {
            var trimmed = style.Trim();
            if (_sb.Length > 0 && !_sb.ToString().TrimEnd().EndsWith(';'))
                _sb.Append("; ");
            else if (_sb.Length > 0)
                _sb.Append(' ');

            _sb.Append(trimmed);
            if (!trimmed.EndsWith(';'))
                _sb.Append(';');
        }
        return this;
    }

    public StyleBuilder AddStyle(string property, string? value, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(property) && !string.IsNullOrWhiteSpace(value))
        {
            AddStyle($"{property}: {value};");
        }
        return this;
    }

    public StyleBuilder AddRaw(string? rawStyle, bool condition = true) => AddStyle(rawStyle, condition);

    public string Build() => _sb.ToString();
    public override string ToString() => Build();
}
