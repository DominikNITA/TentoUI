namespace TentoUI;

/// <summary>
/// Dark theme color variants and overrides.
/// </summary>
public class DarkThemeOptions
{
    public string? AppBgColor { get; set; }
    public string? SurfaceBgColor { get; set; }
    public string? SubtleBgColor { get; set; }
    public string? BaseTextColor { get; set; }
    public string? MutedTextColor { get; set; }
    public string? BaseBorderColor { get; set; }

    public string? PrimaryColor { get; set; }
    public string? PrimaryHoverColor { get; set; }
    public string? SuccessColor { get; set; }
    public string? WarningColor { get; set; }
    public string? ErrorColor { get; set; }
    public string? InfoColor { get; set; }

    public Dictionary<string, string> CustomTokens { get; set; } = new();
}

public class TentoTheme
{
    // Light / Default Colors
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

    // Dark Theme Options
    public DarkThemeOptions Dark { get; set; } = new();

    // Direct property shortcuts for dark variants
    public string? DarkAppBgColor { get => Dark.AppBgColor; set => Dark.AppBgColor = value; }
    public string? DarkSurfaceBgColor { get => Dark.SurfaceBgColor; set => Dark.SurfaceBgColor = value; }
    public string? DarkSubtleBgColor { get => Dark.SubtleBgColor; set => Dark.SubtleBgColor = value; }
    public string? DarkBaseTextColor { get => Dark.BaseTextColor; set => Dark.BaseTextColor = value; }
    public string? DarkMutedTextColor { get => Dark.MutedTextColor; set => Dark.MutedTextColor = value; }
    public string? DarkBaseBorderColor { get => Dark.BaseBorderColor; set => Dark.BaseBorderColor = value; }
    public string? DarkPrimaryColor { get => Dark.PrimaryColor; set => Dark.PrimaryColor = value; }
    public string? DarkPrimaryHoverColor { get => Dark.PrimaryHoverColor; set => Dark.PrimaryHoverColor = value; }
    public string? DarkSuccessColor { get => Dark.SuccessColor; set => Dark.SuccessColor = value; }
    public string? DarkWarningColor { get => Dark.WarningColor; set => Dark.WarningColor = value; }
    public string? DarkErrorColor { get => Dark.ErrorColor; set => Dark.ErrorColor = value; }
    public string? DarkInfoColor { get => Dark.InfoColor; set => Dark.InfoColor = value; }

    public static TentoTheme FromDesignMarkdown(string markdown) => DesignTokensParser.Parse(markdown).ToTentoTheme();

    public string ToStyleString() => ToCssStyle();

    public string ToCssStyle()
    {
        var sb = new System.Text.StringBuilder();

        if (DarkMode)
        {
            sb.Append($"--tento-bg-app: {Dark.AppBgColor ?? "#090d16"}; ");
            sb.Append($"--tento-bg-surface: {Dark.SurfaceBgColor ?? "#111827"}; ");
            sb.Append($"--tento-bg-subtle: {Dark.SubtleBgColor ?? "#1f2937"}; ");
            sb.Append($"--tento-text-base: {Dark.BaseTextColor ?? "#f9fafb"}; ");
            sb.Append($"--tento-text-muted: {Dark.MutedTextColor ?? "#9ca3af"}; ");
            sb.Append($"--tento-text-inverse: {Dark.BaseTextColor ?? "#111827"}; ");
            sb.Append($"--tento-border-base: {Dark.BaseBorderColor ?? "#1f2937"}; ");

            var primary = Dark.PrimaryColor ?? PrimaryColor;
            var primaryHover = Dark.PrimaryHoverColor ?? PrimaryHoverColor;
            var success = Dark.SuccessColor ?? SuccessColor;
            var warning = Dark.WarningColor ?? WarningColor;
            var error = Dark.ErrorColor ?? ErrorColor;
            var info = Dark.InfoColor ?? InfoColor;

            if (!string.IsNullOrWhiteSpace(primary))
                sb.Append($"--tento-primary-500: {primary}; ");
            if (!string.IsNullOrWhiteSpace(primaryHover))
                sb.Append($"--tento-primary-600: {primaryHover}; ");
            if (!string.IsNullOrWhiteSpace(success))
                sb.Append($"--tento-success: {success}; ");
            if (!string.IsNullOrWhiteSpace(warning))
                sb.Append($"--tento-warning: {warning}; ");
            if (!string.IsNullOrWhiteSpace(error))
                sb.Append($"--tento-error: {error}; ");
            if (!string.IsNullOrWhiteSpace(info))
                sb.Append($"--tento-info: {info}; ");
        }
        else
        {
            sb.Append($"--tento-bg-app: {AppBgColor}; ");
            sb.Append($"--tento-bg-surface: {SurfaceBgColor}; ");
            sb.Append($"--tento-bg-subtle: {SubtleBgColor}; ");
            sb.Append($"--tento-text-base: {BaseTextColor}; ");
            sb.Append($"--tento-text-muted: {MutedTextColor}; ");
            sb.Append($"--tento-border-base: {BaseBorderColor}; ");

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
        }

        if (!string.IsNullOrWhiteSpace(BorderRadius))
            sb.Append($"--tento-radius-md: {BorderRadius}; ");
        if (!string.IsNullOrWhiteSpace(FontFamily))
            sb.Append($"--tento-font-family: {FontFamily}; ");

        var tokens = DarkMode && Dark.CustomTokens.Count > 0
            ? MergeTokens(CustomTokens, Dark.CustomTokens)
            : CustomTokens;

        foreach (var (key, value) in tokens)
        {
            var tokenKey = key.StartsWith("--") ? key : $"--{key}";
            sb.Append($"{tokenKey}: {value}; ");
        }

        return sb.ToString().TrimEnd();
    }

    private static Dictionary<string, string> MergeTokens(Dictionary<string, string> baseTokens, Dictionary<string, string> darkTokens)
    {
        var merged = new Dictionary<string, string>(baseTokens, StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in darkTokens)
        {
            merged[k] = v;
        }
        return merged;
    }
}
