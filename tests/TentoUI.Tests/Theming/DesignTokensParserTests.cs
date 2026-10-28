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

        var style = theme.ToCssStyle();
        Assert.Contains("--tento-primary-500: #a5ed6e", style);
        Assert.Contains("--tento-bg-app: #ddf4ff", style);
        Assert.Contains("--tento-radius-md: 12px", style);
    }

    [Fact]
    public void Generators_Produce_ValidOutputs()
    {
        var tokens = DesignTokensParser.Parse(SampleDesignMd);

        var csharp = tokens.ToCSharpCode();
        Assert.Contains("theme.PrimaryColor = \"#a5ed6e\";", csharp);

        var css = tokens.ToCssVariables();
        Assert.Contains("--tento-primary-500: #a5ed6e;", css);

        var json = tokens.ToJsonConfig();
        Assert.Contains("\"PrimaryColor\": \"#a5ed6e\"", json);
    }
}
