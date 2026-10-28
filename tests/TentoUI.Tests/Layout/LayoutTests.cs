namespace TentoUI.Tests.Layout;

public class LayoutTests : TestContext
{
    [Fact]
    public void Layout_WithoutSider_RendersFlexColumnSection()
    {
        var cut = RenderComponent<TentoUI.Layout>(parameters => parameters
            .AddChildContent("<div>Content</div>"));

        var section = cut.Find("section");
        section.ClassList.Should().Contain("flex-col");
    }

    [Fact]
    public void Layout_WithSider_RendersFlexRowSection()
    {
        var cut = RenderComponent<TentoUI.Layout>(parameters => parameters
            .AddChildContent(builder =>
            {
                builder.OpenComponent<Sider>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(2, "Sidebar")));
                builder.CloseComponent();
            }));

        var section = cut.Find("section");
        section.ClassList.Should().Contain("flex-row");
    }

    [Fact]
    public void Sider_WhenCollapsible_RendersWithSpecifiedWidthsAndAriaExpanded()
    {
        var cut = RenderComponent<Sider>(parameters => parameters
            .Add(p => p.Collapsible, true)
            .Add(p => p.Collapsed, false)
            .Add(p => p.Width, "240px")
            .Add(p => p.CollapsedWidth, "80px")
            .AddChildContent("Sidebar Content"));

        var aside = cut.Find("aside");
        aside.GetAttribute("style").Should().Contain("width: 240px");
        aside.GetAttribute("aria-expanded").Should().Be("true");
    }

    [Fact]
    public void Sider_WhenCollapsed_UsesCollapsedWidth()
    {
        var cut = RenderComponent<Sider>(parameters => parameters
            .Add(p => p.Collapsible, true)
            .Add(p => p.Collapsed, true)
            .Add(p => p.Width, "240px")
            .Add(p => p.CollapsedWidth, "80px")
            .AddChildContent("Sidebar Content"));

        var aside = cut.Find("aside");
        aside.GetAttribute("style").Should().Contain("width: 80px");
        aside.GetAttribute("aria-expanded").Should().Be("false");
    }

    [Fact]
    public void HeaderFooterAndContent_RenderSemanticTags()
    {
        var headerCut = RenderComponent<Header>(p => p.AddChildContent("Header Content"));
        headerCut.Find("header").Should().NotBeNull();

        var footerCut = RenderComponent<Footer>(p => p.AddChildContent("Footer Content"));
        footerCut.Find("footer").Should().NotBeNull();

        var contentCut = RenderComponent<Content>(p => p.AddChildContent("Main Body"));
        contentCut.Find("main").Should().NotBeNull();
        contentCut.Find("main").ClassList.Should().Contain("flex-1");
    }

    [Fact]
    public void Grid_RowAndCol_Render24ColumnLayoutClasses()
    {
        var cut = RenderComponent<Row>(parameters => parameters
            .Add(p => p.Gutter, (16, 16))
            .AddChildContent(builder =>
            {
                builder.OpenComponent<Col>(0);
                builder.AddAttribute(1, "Span", 12);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(3, "Column Half")));
                builder.CloseComponent();
            }));

        var row = cut.Find(".tento-row");
        row.ClassList.Should().Contain("flex");
        row.ClassList.Should().Contain("flex-wrap");

        var col = cut.Find(".tento-col");
        col.ClassList.Should().Contain("w-1/2");
    }

    [Fact]
    public void Space_RendersFlexContainerWithGap()
    {
        var cut = RenderComponent<Space>(parameters => parameters
            .Add(p => p.Direction, SpaceDirection.Horizontal)
            .Add(p => p.Size, SpaceSize.Middle)
            .AddChildContent("<span>A</span><span>B</span>"));

        var space = cut.Find(".tento-space");
        space.ClassList.Should().Contain("inline-flex");
        space.ClassList.Should().Contain("gap-4");
    }
}
