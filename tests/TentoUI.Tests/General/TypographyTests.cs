namespace TentoUI.Tests.General;

public class TypographyTests : TestContext
{
    [Theory]
    [InlineData(1, "h1", "text-4xl")]
    [InlineData(2, "h2", "text-3xl")]
    [InlineData(3, "h3", "text-2xl")]
    [InlineData(4, "h4", "text-xl")]
    [InlineData(5, "h5", "text-lg")]
    [InlineData(6, "h6", "text-base")]
    public void Title_Levels_RenderCorrespondingHtmlHeadingAndTailwindSize(int level, string expectedTag, string expectedSizeClass)
    {
        var cut = RenderComponent<Title>(parameters => parameters
            .Add(p => p.Level, level)
            .AddChildContent("Heading Text"));

        var el = cut.Find(expectedTag);
        el.ClassList.Should().Contain(expectedSizeClass);
        el.ClassList.Should().Contain("tento-typography-title");
        el.TextContent.Should().Be("Heading Text");
    }

    [Theory]
    [InlineData(TypographyType.Secondary, "text-slate-500")]
    [InlineData(TypographyType.Success, "text-emerald-500")]
    [InlineData(TypographyType.Warning, "text-amber-500")]
    [InlineData(TypographyType.Danger, "text-red-500")]
    public void Title_Types_ApplyColorClasses(TypographyType type, string expectedClass)
    {
        var cut = RenderComponent<Title>(parameters => parameters
            .Add(p => p.Type, type)
            .AddChildContent("Colored Heading"));

        var el = cut.Find("h1");
        el.ClassList.Should().Contain(expectedClass);
    }

    [Fact]
    public void Text_FormatFlags_WrapContentInSemanticHtmlTags()
    {
        var cut = RenderComponent<Text>(parameters => parameters
            .Add(p => p.Code, true)
            .Add(p => p.Strong, true)
            .AddChildContent("const x = 42;"));

        cut.Find("code").Should().NotBeNull();
        cut.Find("strong").Should().NotBeNull();
        cut.Find("span").TextContent.Should().Contain("const x = 42;");
    }

    [Fact]
    public void Paragraph_RendersParagraphTagWithSpacing()
    {
        var cut = RenderComponent<Paragraph>(parameters => parameters
            .AddChildContent("A long paragraph of text."));

        var p = cut.Find("p");
        p.ClassList.Should().Contain("tento-typography-paragraph");
        p.TextContent.Should().Be("A long paragraph of text.");
    }

    [Fact]
    public void Link_RendersAnchorTagWithHrefAndTarget()
    {
        var cut = RenderComponent<Link>(parameters => parameters
            .Add(p => p.Href, "https://github.com")
            .Add(p => p.Target, "_blank")
            .AddChildContent("GitHub"));

        var link = cut.Find("a");
        link.GetAttribute("href").Should().Be("https://github.com");
        link.GetAttribute("target").Should().Be("_blank");
        link.ClassList.Should().Contain("text-primary-600");
    }
}
