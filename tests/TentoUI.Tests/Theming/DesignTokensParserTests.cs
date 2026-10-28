using TentoUI;
using Xunit;

namespace TentoUI.Tests.Theming;

public class DesignTokensParserTests
{
    private const string SampleDesignMd = """
        ---
        version: alpha
        name: Duolingo
        description: "Free, fun, and effective courses in languages and more."
        sourceUrl: "https://www.duolingo.com"

        colors:
          primary: "#a5ed6e"
          on-primary: "#111111"
          background: "#ddf4ff"
          text: "#3c3c3c"
          text-muted: "#777777"
          accent: "#1cb0f6"
          dark:
            primary: "#58cc02"
            background: "#131f24"
            surface: "#18282e"
            text: "#f7f7f7"
            text-muted: "#8899a6"

        typography:
          display:
            fontFamily: "duolingo-sans, sans-serif"
            fontSize: 15px
            fontWeight: 700
          body:
            fontFamily: "duolingo-sans, sans-serif"
            fontSize: 13px

        radius:
          sm: 12px

        motion:
          easing: "ease"
        ---

        # Duolingo Design System
        Some notes here...
        """;

    [Fact]
    public void Parse_Extracts_CoreTokensCorrectly()
    {
        var tokens = DesignTokensParser.Parse(SampleDesignMd);

        Assert.Equal("Duolingo", tokens.Name);
        Assert.Equal("#a5ed6e", tokens.PrimaryColor);
        Assert.Equal("#111111", tokens.OnPrimaryColor);
        Assert.Equal("#ddf4ff", tokens.BackgroundColor);
        Assert.Equal("#3c3c3c", tokens.TextColor);
        Assert.Equal("#777777", tokens.TextMutedColor);
        Assert.Equal("#1cb0f6", tokens.AccentColor);
        Assert.Equal("12px", tokens.BorderRadius);
        Assert.Equal("duolingo-sans, sans-serif", tokens.FontFamily);

        // Dark variants
        Assert.Equal("#58cc02", tokens.DarkPrimaryColor);
        Assert.Equal("#131f24", tokens.DarkBackgroundColor);
        Assert.Equal("#18282e", tokens.DarkSurfaceColor);
        Assert.Equal("#f7f7f7", tokens.DarkTextColor);
        Assert.Equal("#8899a6", tokens.DarkTextMutedColor);
    }

    [Fact]
    public void ToTentoTheme_Converts_Tokens_To_ValidTheme()
    {
        var theme = TentoTheme.FromDesignMarkdown(SampleDesignMd);

        Assert.Equal("#a5ed6e", theme.PrimaryColor);
        Assert.Equal("#ddf4ff", theme.AppBgColor);
        Assert.Equal("#3c3c3c", theme.BaseTextColor);
        Assert.Equal("#777777", theme.MutedTextColor);
        Assert.Equal("#1cb0f6", theme.InfoColor);
        Assert.Equal("12px", theme.BorderRadius);
        Assert.Equal("duolingo-sans, sans-serif", theme.FontFamily);

        // Dark mode disabled: should emit light tokens
        theme.DarkMode = false;
        var lightStyle = theme.ToCssStyle();
        Assert.Contains("--tento-primary-500: #a5ed6e", lightStyle);
        Assert.Contains("--tento-bg-app: #ddf4ff", lightStyle);
        Assert.Contains("--tento-radius-md: 12px", lightStyle);

        // Dark mode enabled: should emit dark variant tokens
        theme.DarkMode = true;
        var darkStyle = theme.ToCssStyle();
        Assert.Contains("--tento-primary-500: #58cc02", darkStyle);
        Assert.Contains("--tento-bg-app: #131f24", darkStyle);
        Assert.Contains("--tento-bg-surface: #18282e", darkStyle);
        Assert.Contains("--tento-text-base: #f7f7f7", darkStyle);
    }

    [Fact]
    public void BentoTheme_DirectDarkVariant_Overrides_FallbackWhenNull()
    {
        var theme = new TentoTheme
        {
            PrimaryColor = "#2563eb",
            AppBgColor = "#f8fafc",
            Dark =
            {
                PrimaryColor = "#60a5fa",
                AppBgColor = "#0f172a"
            }
        };

        theme.DarkMode = false;
        Assert.Contains("--tento-primary-500: #2563eb", theme.ToCssStyle());
        Assert.Contains("--tento-bg-app: #f8fafc", theme.ToCssStyle());

        theme.DarkMode = true;
        Assert.Contains("--tento-primary-500: #60a5fa", theme.ToCssStyle());
        Assert.Contains("--tento-bg-app: #0f172a", theme.ToCssStyle());
    }

    [Fact]
    public void BentoTheme_DarkVariant_FallsBackToLight_WhenDarkNotSpecified()
    {
        var theme = new TentoTheme
        {
            PrimaryColor = "#2563eb",
            SuccessColor = "#10b981",
            DarkMode = true
        };

        // When Dark.PrimaryColor is not set, it should fallback to PrimaryColor
        var darkStyle = theme.ToCssStyle();
        Assert.Contains("--tento-primary-500: #2563eb", darkStyle);
        Assert.Contains("--tento-success: #10b981", darkStyle);
    }

    [Fact]
    public void Generators_Produce_ValidOutputs()
    {
        var tokens = DesignTokensParser.Parse(SampleDesignMd);

        var csharp = tokens.ToCSharpCode();
        Assert.Contains("theme.PrimaryColor = \"#a5ed6e\";", csharp);
        Assert.Contains("theme.Dark.PrimaryColor = \"#58cc02\";", csharp);

        var css = tokens.ToCssVariables();
        Assert.Contains("--tento-primary-500: #a5ed6e;", css);
        Assert.Contains(".dark, [data-theme=\"dark\"]", css);
        Assert.Contains("--tento-primary-500: #58cc02;", css);

        var json = tokens.ToJsonConfig();
        Assert.Contains("\"PrimaryColor\": \"#a5ed6e\"", json);
        Assert.Contains("\"Dark\"", json);
    }
}
