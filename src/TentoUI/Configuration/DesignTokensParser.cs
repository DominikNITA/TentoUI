using System.Text;
using System.Text.RegularExpressions;

namespace TentoUI;

/// <summary>
/// Parsed design tokens extracted from a Design.md or YAML frontmatter file.
/// </summary>
public record DesignTokens(
    string? Name = null,
    string? Description = null,
    string? PrimaryColor = null,
    string? OnPrimaryColor = null,
    string? BackgroundColor = null,
    string? TextColor = null,
    string? TextMutedColor = null,
    string? AccentColor = null,
    string? SuccessColor = null,
    string? WarningColor = null,
    string? ErrorColor = null,
    string? BorderRadius = null,
    string? FontFamily = null,
    Dictionary<string, string>? AdditionalTokens = null);

/// <summary>
/// Utility for parsing Design.md markdown specifications into TentoUI configurations and themes.
/// </summary>
public static class DesignTokensParser
{
    private static readonly Regex FrontmatterRegex = new(
        @"^---\s*\r?\n(.*?)\r?\n---",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Parses a Design.md file (or raw YAML string) into a structured <see cref="DesignTokens"/> model.
    /// </summary>
    public static DesignTokens Parse(string markdownOrYaml)
    {
        if (string.IsNullOrWhiteSpace(markdownOrYaml))
            return new DesignTokens();

        var yamlContent = markdownOrYaml;
        var match = FrontmatterRegex.Match(markdownOrYaml);
        if (match.Success)
        {
            yamlContent = match.Groups[1].Value;
        }

        var lines = yamlContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var currentSection = "";
        var name = "";
        var description = "";
        var primary = "";
        var onPrimary = "";
        var bg = "";
        var text = "";
        var textMuted = "";
        var accent = "";
        var success = "";
        var warning = "";
        var error = "";
        var radius = "";
        var font = "";
        var additional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Indentation determines nesting
            var indent = rawLine.Length - rawLine.TrimStart().Length;
            var trimmed = line.Trim();

            if (indent == 0 && trimmed.EndsWith(':'))
            {
                currentSection = trimmed[..^1].Trim().ToLowerInvariant();
                continue;
            }

            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx > 0)
            {
                var key = trimmed[..colonIdx].Trim().ToLowerInvariant();
                var val = trimmed[(colonIdx + 1)..].Trim().Trim('"', '\'');

                if (indent == 0)
                {
                    if (key == "name") name = val;
                    else if (key == "description") description = val;
                }
                else if (currentSection == "colors")
                {
                    switch (key)
                    {
                        case "primary": primary = val; break;
                        case "on-primary": onPrimary = val; break;
                        case "background": bg = val; break;
                        case "text": text = val; break;
                        case "text-muted": textMuted = val; break;
                        case "accent": accent = val; break;
                        case "success": success = val; break;
                        case "warning": warning = val; break;
                        case "error": error = val; break;
                        default:
                            if (!string.IsNullOrEmpty(val))
                                additional[$"color-{key}"] = val;
                            break;
                    }
                }
                else if (currentSection == "radius" && (key == "sm" || key == "md" || key == "base"))
                {
                    if (string.IsNullOrEmpty(radius)) radius = val;
                }
                else if (currentSection == "typography" && key == "fontfamily")
                {
                    if (string.IsNullOrEmpty(font)) font = val;
                }
                else if (!string.IsNullOrEmpty(val))
                {
                    additional[$"{currentSection}-{key}"] = val;
                }
            }
        }

        return new DesignTokens(
            Name: string.IsNullOrWhiteSpace(name) ? null : name,
            Description: string.IsNullOrWhiteSpace(description) ? null : description,
            PrimaryColor: string.IsNullOrWhiteSpace(primary) ? null : primary,
            OnPrimaryColor: string.IsNullOrWhiteSpace(onPrimary) ? null : onPrimary,
            BackgroundColor: string.IsNullOrWhiteSpace(bg) ? null : bg,
            TextColor: string.IsNullOrWhiteSpace(text) ? null : text,
            TextMutedColor: string.IsNullOrWhiteSpace(textMuted) ? null : textMuted,
            AccentColor: string.IsNullOrWhiteSpace(accent) ? null : accent,
            SuccessColor: string.IsNullOrWhiteSpace(success) ? null : success,
            WarningColor: string.IsNullOrWhiteSpace(warning) ? null : warning,
            ErrorColor: string.IsNullOrWhiteSpace(error) ? null : error,
            BorderRadius: string.IsNullOrWhiteSpace(radius) ? null : radius,
            FontFamily: string.IsNullOrWhiteSpace(font) ? null : font,
            AdditionalTokens: additional);
    }

    /// <summary>
    /// Converts parsed <see cref="DesignTokens"/> into a <see cref="TentoTheme"/> instance.
    /// </summary>
    public static TentoTheme ToTentoTheme(this DesignTokens tokens)
    {
        var theme = new TentoTheme();

        if (!string.IsNullOrEmpty(tokens.PrimaryColor))
            theme.PrimaryColor = tokens.PrimaryColor;

        if (!string.IsNullOrEmpty(tokens.BackgroundColor))
        {
            theme.AppBgColor = tokens.BackgroundColor;
            theme.SurfaceBgColor = "#ffffff";
        }

        if (!string.IsNullOrEmpty(tokens.TextColor))
            theme.BaseTextColor = tokens.TextColor;

        if (!string.IsNullOrEmpty(tokens.TextMutedColor))
            theme.MutedTextColor = tokens.TextMutedColor;

        if (!string.IsNullOrEmpty(tokens.AccentColor))
            theme.InfoColor = tokens.AccentColor;

        if (!string.IsNullOrEmpty(tokens.SuccessColor))
            theme.SuccessColor = tokens.SuccessColor;

        if (!string.IsNullOrEmpty(tokens.WarningColor))
            theme.WarningColor = tokens.WarningColor;

        if (!string.IsNullOrEmpty(tokens.ErrorColor))
            theme.ErrorColor = tokens.ErrorColor;

        if (!string.IsNullOrEmpty(tokens.BorderRadius))
            theme.BorderRadius = tokens.BorderRadius;

        if (!string.IsNullOrEmpty(tokens.FontFamily))
            theme.FontFamily = tokens.FontFamily;

        if (tokens.AdditionalTokens != null)
        {
            foreach (var (k, v) in tokens.AdditionalTokens)
            {
                theme.CustomTokens[k] = v;
            }
        }

        return theme;
    }

    /// <summary>
    /// Generates C# configuration code for Program.cs.
    /// </summary>
    public static string ToCSharpCode(this DesignTokens tokens)
    {
        var sb = new StringBuilder();
        sb.AppendLine("builder.Services.AddTentoUI(theme =>");
        sb.AppendLine("{");
        if (!string.IsNullOrEmpty(tokens.PrimaryColor))
            sb.AppendLine($"    theme.PrimaryColor = \"{tokens.PrimaryColor}\";");
        if (!string.IsNullOrEmpty(tokens.BackgroundColor))
            sb.AppendLine($"    theme.AppBgColor = \"{tokens.BackgroundColor}\";");
        if (!string.IsNullOrEmpty(tokens.TextColor))
            sb.AppendLine($"    theme.BaseTextColor = \"{tokens.TextColor}\";");
        if (!string.IsNullOrEmpty(tokens.TextMutedColor))
            sb.AppendLine($"    theme.MutedTextColor = \"{tokens.TextMutedColor}\";");
        if (!string.IsNullOrEmpty(tokens.AccentColor))
            sb.AppendLine($"    theme.InfoColor = \"{tokens.AccentColor}\";");
        if (!string.IsNullOrEmpty(tokens.BorderRadius))
            sb.AppendLine($"    theme.BorderRadius = \"{tokens.BorderRadius}\";");
        if (!string.IsNullOrEmpty(tokens.FontFamily))
            sb.AppendLine($"    theme.FontFamily = \"{tokens.FontFamily}\";");
        sb.AppendLine("});");
        return sb.ToString();
    }

    /// <summary>
    /// Generates CSS variables stylesheet.
    /// </summary>
    public static string ToCssVariables(this DesignTokens tokens)
    {
        var sb = new StringBuilder();
        sb.AppendLine(":root {");
        if (!string.IsNullOrEmpty(tokens.PrimaryColor))
            sb.AppendLine($"  --tento-primary-500: {tokens.PrimaryColor};");
        if (!string.IsNullOrEmpty(tokens.BackgroundColor))
            sb.AppendLine($"  --tento-bg-app: {tokens.BackgroundColor};");
        if (!string.IsNullOrEmpty(tokens.TextColor))
            sb.AppendLine($"  --tento-text-base: {tokens.TextColor};");
        if (!string.IsNullOrEmpty(tokens.TextMutedColor))
            sb.AppendLine($"  --tento-text-muted: {tokens.TextMutedColor};");
        if (!string.IsNullOrEmpty(tokens.AccentColor))
            sb.AppendLine($"  --tento-info: {tokens.AccentColor};");
        if (!string.IsNullOrEmpty(tokens.BorderRadius))
            sb.AppendLine($"  --tento-radius-md: {tokens.BorderRadius};");
        if (!string.IsNullOrEmpty(tokens.FontFamily))
            sb.AppendLine($"  --tento-font-family: {tokens.FontFamily};");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Generates appsettings.json configuration fragment.
    /// </summary>
    public static string ToJsonConfig(this DesignTokens tokens)
    {
        var sb = new StringBuilder();
        sb.AppendLine("  \"TentoTheme\": {");
        var items = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(tokens.PrimaryColor))
            items.Add($"    \"PrimaryColor\": \"{tokens.PrimaryColor}\"");
        if (!string.IsNullOrEmpty(tokens.BackgroundColor))
            items.Add($"    \"AppBgColor\": \"{tokens.BackgroundColor}\"");
        if (!string.IsNullOrEmpty(tokens.TextColor))
            items.Add($"    \"BaseTextColor\": \"{tokens.TextColor}\"");
        if (!string.IsNullOrEmpty(tokens.TextMutedColor))
            items.Add($"    \"MutedTextColor\": \"{tokens.TextMutedColor}\"");
        if (!string.IsNullOrEmpty(tokens.AccentColor))
            items.Add($"    \"InfoColor\": \"{tokens.AccentColor}\"");
        if (!string.IsNullOrEmpty(tokens.BorderRadius))
            items.Add($"    \"BorderRadius\": \"{tokens.BorderRadius}\"");
        if (!string.IsNullOrEmpty(tokens.FontFamily))
            items.Add($"    \"FontFamily\": \"{tokens.FontFamily}\"");
        sb.AppendLine(string.Join(",\n", items));
        sb.AppendLine("  }");
        return sb.ToString();
    }
}
