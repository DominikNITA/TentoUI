namespace TentoUI.Tests.General;

public class ButtonTests : TestContext
{
    [Theory]
    [InlineData(ButtonType.Primary, "tento-btn-primary", "bg-primary-500")]
    [InlineData(ButtonType.Default, "tento-btn-default", "border")]
    [InlineData(ButtonType.Dashed, "tento-btn-dashed", "border-dashed")]
    [InlineData(ButtonType.Text, "tento-btn-text", "bg-transparent")]
    [InlineData(ButtonType.Link, "tento-btn-link", "underline")]
    public void Button_SemanticVariants_ApplyCorrectCssClasses(ButtonType type, string expectedClass1, string expectedClass2)
    {
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Type, type)
            .AddChildContent("Click Me"));

        var btn = cut.Find("button");
        btn.ClassList.Should().Contain(expectedClass1);
        btn.ClassList.Should().Contain(expectedClass2);
    }

    [Theory]
    [InlineData(ComponentSize.Small, "px-2.5", "py-1.5", "text-xs")]
    [InlineData(ComponentSize.Middle, "px-4", "py-2", "text-sm")]
    [InlineData(ComponentSize.Large, "px-6", "py-3", "text-base")]
    public void Button_Sizes_ApplyCorrectPaddingAndTypography(ComponentSize size, string px, string py, string text)
    {
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Size, size)
            .AddChildContent("Size Test"));

        var btn = cut.Find("button");
        btn.ClassList.Should().Contain(px);
        btn.ClassList.Should().Contain(py);
        btn.ClassList.Should().Contain(text);
    }

    [Fact]
    public void Button_WhenLoadingIsTrue_IncludesSpinnerAndAriaBusy()
    {
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Loading, true)
            .AddChildContent("Submit"));

        var btn = cut.Find("button");
        btn.HasAttribute("disabled").Should().BeTrue();
        btn.GetAttribute("aria-busy").Should().Be("true");
        cut.Find(".animate-spin").Should().NotBeNull();
    }

    [Fact]
    public void Button_WhenDisabled_SuppressesClickAndHasDisabledAttribute()
    {
        var clicked = false;
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.OnClick, () => clicked = true)
            .AddChildContent("Disabled"));

        var btn = cut.Find("button");
        btn.HasAttribute("disabled").Should().BeTrue();
        btn.ClassList.Should().Contain("opacity-50");
        btn.ClassList.Should().Contain("cursor-not-allowed");

        btn.Click();
        clicked.Should().BeFalse();
    }

    [Fact]
    public void Button_WhenClicked_InvokesOnClickCallback()
    {
        var clicked = false;
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.OnClick, () => clicked = true)
            .AddChildContent("Active"));

        cut.Find("button").Click();
        clicked.Should().BeTrue();
    }

    [Fact]
    public void Button_WhenDangerFlagIsTrue_AppliesDestructiveColors()
    {
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Type, ButtonType.Primary)
            .Add(p => p.Danger, true)
            .AddChildContent("Delete"));

        var btn = cut.Find("button");
        btn.ClassList.Should().Contain("bg-red-500");
    }

    [Fact]
    public void Button_WithHref_RendersAnchorTag()
    {
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Href, "https://example.com")
            .AddChildContent("Link Button"));

        var link = cut.Find("a");
        link.GetAttribute("href").Should().Be("https://example.com");
        link.GetAttribute("role").Should().Be("button");
    }
}
