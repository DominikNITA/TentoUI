using Bunit;
using FluentAssertions;
using TentoUI.Docs.Components;
using Xunit;

namespace TentoUI.Tests.Components;

public class TentoMarkupRendererTests : TestContext
{
    [Fact]
    public void TentoMarkupRenderer_RendersCarousel_CorrectlyWithPanelsAndAutoplay()
    {
        // Arrange
        const string markup = """
            <Carousel AutoPlay="true" AutoPlaySpeed="4000" Dots="true" Arrows="true">
                <CarouselPanel>
                    <div class="test-slide-1">Slide 1: Modern Blazor UI</div>
                </CarouselPanel>
                <CarouselPanel>
                    <div class="test-slide-2">Slide 2: WebAssembly Ready</div>
                </CarouselPanel>
            </Carousel>
            """;

        // Act
        var cut = RenderComponent<TentoMarkupRenderer>(parameters => parameters
            .Add(p => p.Markup, markup));

        // Assert
        cut.Find(".tento-carousel").Should().NotBeNull();
        cut.Find(".tento-carousel-viewport").Should().NotBeNull();
        cut.Find(".tento-carousel-dots").Should().NotBeNull();
        cut.Find(".tento-carousel-prev").Should().NotBeNull();
        cut.Find(".tento-carousel-next").Should().NotBeNull();
        cut.Markup.Should().Contain("Slide 1: Modern Blazor UI");
    }

    [Fact]
    public void TentoMarkupRenderer_RendersButton_WithDynamicAttributes()
    {
        // Arrange
        const string markup = """<Button Type="ButtonType.Primary" Danger="true">Destructive Action</Button>""";

        // Act
        var cut = RenderComponent<TentoMarkupRenderer>(parameters => parameters
            .Add(p => p.Markup, markup));

        // Assert
        cut.Find("button").Should().NotBeNull();
        cut.Find("button").TextContent.Trim().Should().Be("Destructive Action");
        cut.Markup.Should().Contain("bg-red-500");
    }

    [Fact]
    public void TentoMarkupRenderer_RendersStatistic_WithNamedFragments()
    {
        // Arrange
        const string markup = """
            <Statistic Title="Revenue Run Rate" Value="142000">
                <Prefix>$</Prefix>
                <Suffix>USD</Suffix>
            </Statistic>
            """;

        // Act
        var cut = RenderComponent<TentoMarkupRenderer>(parameters => parameters
            .Add(p => p.Markup, markup));

        // Assert
        cut.Find(".tento-statistic").Should().NotBeNull();
        cut.Find(".tento-statistic-title").TextContent.Trim().Should().Be("Revenue Run Rate");
        cut.Find(".tento-statistic-prefix").TextContent.Trim().Should().Be("$");
        cut.Find(".tento-statistic-suffix").TextContent.Trim().Should().Be("USD");
        cut.Find(".tento-statistic-value").TextContent.Trim().Should().Be("142000");
    }

    [Fact]
    public void TentoMarkupRenderer_HandlesNestedDivsAndCards()
    {
        // Arrange
        const string markup = """
            <Card Title="Security Status">
                <div class="nested-box">
                    <span class="font-bold">2FA Active</span>
                </div>
            </Card>
            """;

        // Act
        var cut = RenderComponent<TentoMarkupRenderer>(parameters => parameters
            .Add(p => p.Markup, markup));

        // Assert
        cut.Find(".tento-card").Should().NotBeNull();
        cut.Markup.Should().Contain("Security Status");
        cut.Markup.Should().Contain("2FA Active");
    }

    [Fact]
    public void TentoMarkupRenderer_RendersTimeline_WithItems()
    {
        // Arrange
        const string markup = """
            <Timeline>
                <TimelineItem Color="green" Label="09:00">Task Completed</TimelineItem>
                <TimelineItem Color="blue" Label="10:00">In Progress</TimelineItem>
            </Timeline>
            """;

        // Act
        var cut = RenderComponent<TentoMarkupRenderer>(parameters => parameters
            .Add(p => p.Markup, markup));

        // Assert
        cut.Find(".tento-timeline").Should().NotBeNull();
        cut.Markup.Should().Contain("Task Completed");
        cut.Markup.Should().Contain("In Progress");
        cut.Markup.Should().Contain("09:00");
        cut.Markup.Should().Contain("10:00");
    }

    [Fact]
    public void DemoCard_RendersPreview_DirectlyFromCodePropertyInitially()
    {
        // Arrange
        Services.AddTentoUI();
        const string code = """<Button Type="ButtonType.Primary">Action From Code</Button>""";

        // Act
        var cut = RenderComponent<DemoCard>(parameters => parameters
            .Add(p => p.Title, "Live Button Demo")
            .Add(p => p.Code, code));

        // Assert - The preview area must render the component defined in Code immediately
        var preview = cut.Find(".tento-demo-preview");
        preview.TextContent.Should().Contain("Action From Code");
    }

    [Fact]
    public void DemoCard_RendersCarousel_DirectlyFromCodePropertyInitially()
    {
        // Arrange
        Services.AddTentoUI();
        const string code = """
            <Carousel Dots="true">
                <CarouselPanel>
                    <div>Direct Slide 1</div>
                </CarouselPanel>
            </Carousel>
            """;

        // Act
        var cut = RenderComponent<DemoCard>(parameters => parameters
            .Add(p => p.Title, "Live Carousel Demo")
            .Add(p => p.Code, code));

        // Assert - The preview must render the Carousel directly from Code
        cut.Find(".tento-carousel").Should().NotBeNull();
        cut.Markup.Should().Contain("Direct Slide 1");
    }

    [Fact]
    public void TentoMarkupRenderer_RendersSegmented_WithDeclarativeItems()
    {
        // Arrange
        const string markup = """
            <Segmented TValue="string" Value="Daily">
                <SegmentedItem Value="Daily">Daily</SegmentedItem>
                <SegmentedItem Value="Weekly">Weekly</SegmentedItem>
                <SegmentedItem Value="Monthly">Monthly</SegmentedItem>
            </Segmented>
            """;

        // Act
        var cut = RenderComponent<TentoMarkupRenderer>(parameters => parameters
            .Add(p => p.Markup, markup));

        // Assert
        cut.Find(".tento-segmented").Should().NotBeNull();
        var items = cut.FindAll(".tento-segmented-item");
        items.Should().HaveCount(3);
        items[0].TextContent.Trim().Should().Be("Daily");
        items[1].TextContent.Trim().Should().Be("Weekly");
        items[2].TextContent.Trim().Should().Be("Monthly");
    }

    [Fact]
    public void TentoMarkupRenderer_RendersSegmented_WithStringOptions()
    {
        // Arrange
        const string markup = """<Segmented TValue="string" Value="Map" Options="Map, Transit, Satellite" />""";

        // Act
        var cut = RenderComponent<TentoMarkupRenderer>(parameters => parameters
            .Add(p => p.Markup, markup));

        // Assert
        cut.Find(".tento-segmented").Should().NotBeNull();
        var items = cut.FindAll(".tento-segmented-item");
        items.Should().HaveCount(3);
        items[0].TextContent.Trim().Should().Be("Map");
        items[1].TextContent.Trim().Should().Be("Transit");
        items[2].TextContent.Trim().Should().Be("Satellite");
    }

    [Fact]
    public void DemoCard_RendersSegmented_DirectlyFromCodePropertyInitially()
    {
        // Arrange
        Services.AddTentoUI();
        const string code = """
            <Segmented TValue="string" Value="Monthly">
                <SegmentedItem Value="Daily">Daily</SegmentedItem>
                <SegmentedItem Value="Monthly">Monthly</SegmentedItem>
            </Segmented>
            """;

        // Act
        var cut = RenderComponent<DemoCard>(parameters => parameters
            .Add(p => p.Title, "Live Segmented Demo")
            .Add(p => p.Code, code));

        // Assert - The preview area must render the segmented control with options immediately
        cut.Find(".tento-segmented").Should().NotBeNull();
        var items = cut.FindAll(".tento-segmented-item");
        items.Should().HaveCount(2);
        items[1].TextContent.Trim().Should().Be("Monthly");
        items[1].GetAttribute("aria-checked").Should().Be("true");
    }

    [Fact]
    public void TentoMarkupRenderer_RendersSelect_WithStringOptions()
    {
        // Arrange
        const string markup = """<Select TItem="string" TValue="string" Placeholder="Choose..." Options="Warsaw, Berlin, London" />""";

        // Act
        var cut = RenderComponent<TentoMarkupRenderer>(parameters => parameters
            .Add(p => p.Markup, markup));

        // Assert
        cut.Find(".tento-select").Should().NotBeNull();
        cut.Markup.Should().Contain("Choose...");
    }

    [Fact]
    public void TentoMarkupRenderer_RendersAutoComplete_WithOptionsString()
    {
        // Arrange
        const string markup = """<AutoComplete Placeholder="Search email..." OptionsString="gmail.com, outlook.com, yahoo.com" />""";

        // Act
        var cut = RenderComponent<TentoMarkupRenderer>(parameters => parameters
            .Add(p => p.Markup, markup));

        // Assert
        cut.Find(".tento-autocomplete").Should().NotBeNull();
        cut.Find("input").GetAttribute("placeholder").Should().Be("Search email...");
    }

    [Fact]
    public void TentoMarkupRenderer_RendersList_WithDeclarativeItems()
    {
        // Arrange
        const string markup = """
            <List Bordered="true">
                <ListItem>Task One</ListItem>
                <ListItem>Task Two</ListItem>
            </List>
            """;

        // Act
        var cut = RenderComponent<TentoMarkupRenderer>(parameters => parameters
            .Add(p => p.Markup, markup));

        // Assert
        cut.Find(".tento-list").Should().NotBeNull();
        cut.Markup.Should().Contain("Task One");
        cut.Markup.Should().Contain("Task Two");
    }
}
