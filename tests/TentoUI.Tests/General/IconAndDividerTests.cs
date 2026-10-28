namespace TentoUI.Tests.General;

public class IconAndDividerTests : TestContext
{
    [Fact]
    public void Icon_KnownIconName_RendersSvgPath()
    {
        var cut = RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, IconNames.Check)
            .Add(p => p.Size, 20)
            .Add(p => p.AriaLabel, "Completed successfully"));

        var svg = cut.Find("svg");
        svg.GetAttribute("width").Should().Be("20");
        svg.GetAttribute("height").Should().Be("20");
        svg.GetAttribute("aria-label").Should().Be("Completed successfully");
        svg.InnerHtml.Should().Contain("path");
    }

    [Fact]
    public void Icon_WithSpin_AddsAnimateSpinClass()
    {
        var cut = RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, IconNames.Loader)
            .Add(p => p.Spin, true));

        var svg = cut.Find("svg");
        svg.ClassList.Should().Contain("animate-spin");
    }

    [Fact]
    public void Divider_Horizontal_RendersDefaultDivider()
    {
        var cut = RenderComponent<Divider>();
        var el = cut.Find("div");
        el.ClassList.Should().Contain("tento-divider");
        el.ClassList.Should().Contain("w-full");
    }

    [Fact]
    public void Divider_Vertical_RendersVerticalDivider()
    {
        var cut = RenderComponent<Divider>(parameters => parameters
            .Add(p => p.Type, DividerType.Vertical));

        var el = cut.Find("div");
        el.ClassList.Should().Contain("inline-block");
        el.ClassList.Should().Contain("h-4");
    }

    [Fact]
    public void Divider_WithText_RendersChildContent()
    {
        var cut = RenderComponent<Divider>(parameters => parameters
            .Add(p => p.Orientation, DividerOrientation.Center)
            .AddChildContent("OR"));

        cut.Find(".tento-divider-inner-text").TextContent.Should().Be("OR");
    }
}
