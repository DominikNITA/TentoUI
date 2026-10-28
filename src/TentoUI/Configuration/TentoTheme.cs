namespace TentoUI;

public class TentoTheme
{
    public string AppBgColor { get; set; } = "#f8fafc";
    public string SurfaceBgColor { get; set; } = "#ffffff";
    public string SubtleBgColor { get; set; } = "#f1f5f9";
    public string BaseTextColor { get; set; } = "#0f172a";
    public string MutedTextColor { get; set; } = "#64748b";
    public string BaseBorderColor { get; set; } = "#e2e8f0";

    public string PrimaryColor { get; set; } = "#3b82f6";
    public string ColorPrimary { get => PrimaryColor; set => PrimaryColor = value; }
    public string PrimaryHoverColor { get; set; } = "#2563eb";
    public string SuccessColor { get; set; } = "#10b981";
    public string WarningColor { get; set; } = "#f59e0b";
    public string ErrorColor { get; set; } = "#ef4444";
    public string InfoColor { get; set; } = "#0ea5e9";
    public string BorderRadius { get; set; } = "0.375rem";
    public string? FontFamily { get; set; }
    public bool DarkMode { get; set; } = false;
    public Dictionary<string, string> CustomTokens { get; set; } = new();

    public static TentoTheme FromDesignMarkdown(string markdown) => DesignTokensParser.Parse(markdown).ToTentoTheme();

    public string ToStyleString() => ToCssStyle();

    public string ToCssStyle()
    {
        var sb = new System.Text.StringBuilder();

        if (DarkMode)
        {
            sb.Append("--tento-bg-app: #090d16; ");
            sb.Append("--tento-bg-surface: #111827; ");
            sb.Append("--tento-bg-subtle: #1f2937; ");
            sb.Append("--tento-text-base: #f9fafb; ");
            sb.Append("--tento-text-muted: #9ca3af; ");
            sb.Append("--tento-text-inverse: #111827; ");
            sb.Append("--tento-border-base: #1f2937; ");
        }
        else
        {
            sb.Append($"--tento-bg-app: {AppBgColor}; ");
            sb.Append($"--tento-bg-surface: {SurfaceBgColor}; ");
            sb.Append($"--tento-bg-subtle: {SubtleBgColor}; ");
            sb.Append($"--tento-text-base: {BaseTextColor}; ");
            sb.Append($"--tento-text-muted: {MutedTextColor}; ");
            sb.Append($"--tento-border-base: {BaseBorderColor}; ");
        }

        if (!string.IsNullOrWhiteSpace(PrimaryColor))
            sb.Append($"--tento-primary-500: {PrimaryColor}; ");
        if (!string.IsNullOrWhiteSpace(PrimaryHoverColor))
            sb.Append($"--tento-primary-600: {PrimaryHoverColor}; ");
        if (!string.IsNullOrWhiteSpace(SuccessColor))
            sb.Append($"--tento-success: {SuccessColor}; ");
        if (!string.IsNullOrWhiteSpace(WarningColor))
            sb.Append($"--tento-warning: {WarningColor}; ");
        if (!string.IsNullOrWhiteSpace(ErrorColor))
            sb.Append($"--tento-error: {ErrorColor}; ");
        if (!string.IsNullOrWhiteSpace(InfoColor))
            sb.Append($"--tento-info: {InfoColor}; ");
        if (!string.IsNullOrWhiteSpace(BorderRadius))
            sb.Append($"--tento-radius-md: {BorderRadius}; ");
        if (!string.IsNullOrWhiteSpace(FontFamily))
            sb.Append($"--tento-font-family: {FontFamily}; ");

        foreach (var (key, value) in CustomTokens)
        {
            var tokenKey = key.StartsWith("--") ? key : $"--{key}";
            sb.Append($"{tokenKey}: {value}; ");
        }

        return sb.ToString().TrimEnd();
    }
}
