namespace TentoUI.Tests.Theming;

public class ThemeTests : TestContext
{
    [Fact]
    public void TentoTheme_GeneratesAll13CoreCssTokens()
    {
        var theme = new TentoTheme();
        var style = theme.ToStyleString();

        var expectedTokens = new[]
        {
            "--tento-bg-app",
            "--tento-bg-surface",
            "--tento-bg-subtle",
            "--tento-text-base",
            "--tento-text-muted",
            "--tento-border-base",
            "--tento-primary-500",
            "--tento-primary-600",
            "--tento-success",
            "--tento-warning",
            "--tento-error",
            "--tento-info",
            "--tento-radius-md"
        };

        foreach (var token in expectedTokens)
        {
            style.Should().Contain(token, $"Token {token} must be present in theme style string");
        }
    }

    [Fact]
    public void TentoTheme_DarkMode_EmitsDarkPalette()
    {
        var theme = new TentoTheme { DarkMode = true };
        var style = theme.ToStyleString();

        style.Should().Contain("--tento-bg-app: #090d16;");
        style.Should().Contain("--tento-bg-surface: #111827;");
        style.Should().Contain("--tento-text-base: #f9fafb;");
        style.Should().Contain("--tento-text-inverse: #111827;");
    }

    [Fact]
    public void TentoTheme_CustomColorsAndRadius_OverridesDefaults()
    {
        var theme = new TentoTheme
        {
            ColorPrimary = "#8b5cf6",
            BorderRadius = "1rem"
        };
        var style = theme.ToStyleString();

        style.Should().Contain("--tento-primary-500: #8b5cf6;");
        style.Should().Contain("--tento-radius-md: 1rem;");
    }

    [Fact]
    public void TentoConfigProvider_EmitsRootContainerWithThemeAttributes()
    {
        var cut = RenderComponent<TentoConfigProvider>(parameters => parameters
            .Add(p => p.DarkMode, true)
            .Add(p => p.Theme, new TentoTheme { ColorPrimary = "#ec4899" })
            .AddChildContent("<div>App Content</div>"));

        var root = cut.Find("div");
        root.GetAttribute("data-theme").Should().Be("dark");
        root.ClassList.Should().Contain("dark");
        root.GetAttribute("style").Should().Contain("--tento-primary-500: #ec4899;");
        root.InnerHtml.Should().Contain("App Content");
    }

    [Fact]
    public void TentoConfigProvider_UsesInjectedThemeFromDI_WhenThemeParameterNotProvided()
    {
        Services.AddTentoUI(theme =>
        {
            theme.PrimaryColor = "#58cc02";
            theme.AppBgColor = "#ddf4ff";
            theme.BaseTextColor = "#3c3c3c";
            theme.BorderRadius = "16px";
            theme.FontFamily = "duolingo-sans, sans-serif";
        });

        var cut = RenderComponent<TentoConfigProvider>(parameters => parameters
            .AddChildContent("<span>Content</span>"));

        var root = cut.Find("div");
        var style = root.GetAttribute("style");

        style.Should().Contain("--tento-primary-500: #58cc02;");
        style.Should().Contain("--tento-bg-app: #ddf4ff;");
        style.Should().Contain("--tento-text-base: #3c3c3c;");
        style.Should().Contain("--tento-radius-md: 16px;");
        style.Should().Contain("--tento-font-family: duolingo-sans, sans-serif;");
    }

    [Fact]
    public void TentoConfigProvider_ExplicitThemeParameter_OverridesInjectedThemeFromDI()
    {
        Services.AddTentoUI(theme =>
        {
            theme.PrimaryColor = "#58cc02";
        });

        var cut = RenderComponent<TentoConfigProvider>(parameters => parameters
            .Add(p => p.Theme, new TentoTheme { PrimaryColor = "#ff0000" })
            .AddChildContent("<span>Overridden</span>"));

        var root = cut.Find("div");
        var style = root.GetAttribute("style");

        style.Should().Contain("--tento-primary-500: #ff0000;");
        style.Should().NotContain("--tento-primary-500: #58cc02;");
    }

    [Fact]
    public void CssBuilder_MergesAndConditionsClassesAccurately()
    {
        var builder = new CssBuilder("base-class")
            .AddClass("active", true)
            .AddClass("hidden", false)
            .AddClass("custom-class");

        builder.Build().Should().Be("base-class active custom-class");
    }

    [Fact]
    public void StyleBuilder_BuildsInlineCssProperties()
    {
        var style = new StyleBuilder()
            .AddStyle("color", "red", true)
            .AddStyle("display", "none", false)
            .AddRaw("font-size: 14px;")
            .Build();

        style.Should().Contain("color: red;");
        style.Should().NotContain("display: none;");
        style.Should().Contain("font-size: 14px;");
    }
}
