namespace TentoUI.Tests.Components;

public class EnterpriseComponentsTests : TestContext
{
    [Fact]
    public void Tree_ExpandsChecksAndSelectsNodes()
    {
        string? selectedKey = null;
        var checkedKeys = new HashSet<string>();

        var treeData = new[]
        {
            new TreeDataNode("root", "Root Folder", new[]
            {
                new TreeDataNode("child-1", "File 1.txt", isLeaf: true),
                new TreeDataNode("child-2", "File 2.txt", isLeaf: true)
            })
        };

        var cut = RenderComponent<Tree>(parameters => parameters
            .Add(p => p.Data, treeData)
            .Add(p => p.Checkable, true)
            .Add(p => p.ExpandedKeys, new HashSet<string> { "root" })
            .Add(p => p.SelectedKeyChanged, EventCallback.Factory.Create<string>(this, k => selectedKey = k))
            .Add(p => p.CheckedKeys, checkedKeys));

        cut.FindAll(".tento-tree-node").Count.Should().Be(3); // Root + 2 children

        // Select child 1
        cut.Find("li[data-tree-key='child-1'] .tento-tree-title").Click();
        selectedKey.Should().Be("child-1");

        // Check child 2 checkbox
        var child2Checkbox = cut.Find("li[data-tree-key='child-2'] input[type='checkbox']");
        child2Checkbox.Change(true);
        checkedKeys.Should().Contain("child-2");
    }

    [Fact]
    public void TreeSelect_OpensDropdownAndSelectsItem()
    {
        string? selectedVal = null;
        var treeData = new[]
        {
            new TreeDataNode("node-1", "First Choice", isLeaf: true),
            new TreeDataNode("node-2", "Second Choice", isLeaf: true)
        };

        var cut = RenderComponent<TreeSelect>(parameters => parameters
            .Add(p => p.TreeData, treeData)
            .Add(p => p.Value, selectedVal)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => selectedVal = v)));

        cut.FindAll(".tento-treeselect-popup").Should().BeEmpty();

        // Click to open
        cut.Find("input").Click();
        cut.Find(".tento-treeselect-popup").Should().NotBeNull();

        // Select second choice
        cut.Find("li[data-tree-key='node-2'] .tento-tree-title").Click();
        selectedVal.Should().Be("node-2");
    }

    [Fact]
    public void Cascader_DrilldownAndSelection()
    {
        IReadOnlyList<string> selected = Array.Empty<string>();
        var options = new[]
        {
            new CascaderOption("na", "North America", new[]
            {
                new CascaderOption("us", "United States", new[]
                {
                    new CascaderOption("ny", "New York")
                })
            })
        };

        var cut = RenderComponent<Cascader>(parameters => parameters
            .Add(p => p.Options, options)
            .Add(p => p.Value, selected)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(this, v => selected = v)));

        // Open cascader
        cut.Find("input").Click();
        cut.Find(".tento-cascader-popup").Should().NotBeNull();

        // Click North America
        cut.Find("button:contains('North America')").Click();
        // Click United States
        cut.Find("button:contains('United States')").Click();
        // Click New York
        cut.Find("button:contains('New York')").Click();

        selected.Should().ContainInOrder("na", "us", "ny");
    }

    [Fact]
    public void Transfer_MovesItemsBetweenPanels()
    {
        var targetKeys = new HashSet<string> { "item-1" };
        var dataSource = new[]
        {
            new TransferItem("item-1", "First Item"),
            new TransferItem("item-2", "Second Item"),
            new TransferItem("item-3", "Third Item")
        };

        var cut = RenderComponent<Transfer>(parameters => parameters
            .Add(p => p.DataSource, dataSource)
            .Add(p => p.TargetKeys, targetKeys)
            .Add(p => p.TargetKeysChanged, EventCallback.Factory.Create<HashSet<string>>(this, t => targetKeys = t)));

        // Source has item-2 and item-3; Target has item-1
        var sourcePanel = cut.FindAll(".tento-transfer-panel")[0];
        var targetPanel = cut.FindAll(".tento-transfer-panel")[1];

        sourcePanel.TextContent.Should().Contain("Second Item");
        targetPanel.TextContent.Should().Contain("First Item");

        // Check item-2 in source and move right
        var item2Checkbox = cut.Find(".tento-transfer-panel label:contains('Second Item') input[type='checkbox']");
        item2Checkbox.Change(true);

        cut.Find("button[aria-label='Move right']").Click();
        targetKeys.Should().Contain("item-2");
    }

    [Fact]
    public void Watermark_RendersOverlayAndChildContent()
    {
        var cut = RenderComponent<Watermark>(parameters => parameters
            .Add(p => p.Content, "INTERNAL CONFIDENTIAL")
            .AddChildContent("Sensitive Report Body"));

        cut.Markup.Should().Contain("Sensitive Report Body");
        cut.Find(".tento-watermark-overlay").Should().NotBeNull();
    }

    [Fact]
    public void QRCode_RendersSvgAndExpiredStatus()
    {
        bool refreshed = false;
        var cut = RenderComponent<QRCode>(parameters => parameters
            .Add(p => p.Value, "https://example.com")
            .Add(p => p.Status, QRCodeStatus.Expired)
            .Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => refreshed = true)));

        cut.Find("svg").Should().NotBeNull();
        cut.Find("button:contains('Refresh')").Click();
        refreshed.Should().BeTrue();
    }

    [Fact]
    public void Calendar_RendersMonthAndSelectsDate()
    {
        var selectedDate = new DateOnly(2026, 10, 15);
        var cut = RenderComponent<Calendar>(parameters => parameters
            .Add(p => p.Value, selectedDate)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateOnly>(this, d => selectedDate = d)));

        cut.Find(".tento-calendar-header").TextContent.Should().Contain("October 2026");

        // Click next month
        cut.Find("button[aria-label='Next month']").Click();
        cut.Find(".tento-calendar-header").TextContent.Should().Contain("November 2026");
    }

    [Fact]
    public void Tour_NavigatesStepsAndFinishes()
    {
        bool tourOpen = true;
        bool finished = false;

        var cut = RenderComponent<Tour>(parameters => parameters
            .Add(p => p.Open, tourOpen)
            .Add(p => p.OpenChanged, EventCallback.Factory.Create<bool>(this, o => tourOpen = o))
            .Add(p => p.OnFinish, EventCallback.Factory.Create(this, () => finished = true))
            .AddChildContent(builder =>
            {
                builder.OpenComponent<TourStep>(0);
                builder.AddAttribute(1, "Title", "Welcome Step");
                builder.AddAttribute(2, "Description", "Here is step 1.");
                builder.CloseComponent();

                builder.OpenComponent<TourStep>(3);
                builder.AddAttribute(4, "Title", "Final Step");
                builder.AddAttribute(5, "Description", "All done!");
                builder.CloseComponent();
            }));

        cut.Find(".tento-tour-root").Should().NotBeNull();
        cut.Markup.Should().Contain("Welcome Step");

        // Click Next
        cut.Find("button:contains('Next')").Click();
        cut.Markup.Should().Contain("Final Step");

        // Click Finish
        cut.Find("button:contains('Finish')").Click();
        finished.Should().BeTrue();
        tourOpen.Should().BeFalse();
    }

    [Fact]
    public void Tour_WithTarget_RendersTargetBadge()
    {
        var cut = RenderComponent<Tour>(parameters => parameters
            .Add(p => p.Open, true)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<TourStep>(0);
                builder.AddAttribute(1, "Title", "Search Step");
                builder.AddAttribute(2, "Target", "#tour-search");
                builder.CloseComponent();
            }));

        cut.Find(".tento-tour-root").Should().NotBeNull();
        cut.Markup.Should().Contain("Target: #tour-search");
    }

    [Fact]
    public void Anchor_And_Affix_RenderCorrectly()
    {
        string? active = null;
        var cut = RenderComponent<Anchor>(parameters => parameters
            .Add(p => p.ActiveHref, active)
            .Add(p => p.ActiveHrefChanged, EventCallback.Factory.Create<string>(this, a => active = a))
            .AddChildContent(builder =>
            {
                builder.OpenComponent<AnchorLink>(0);
                builder.AddAttribute(1, "Href", "#section-1");
                builder.AddAttribute(2, "Title", "Introduction");
                builder.CloseComponent();
            }));

        cut.Find("a[href='#section-1']").Click();
        active.Should().Be("#section-1");

        var affixCut = RenderComponent<Affix>(p => p
            .Add(x => x.OffsetTop, 20)
            .AddChildContent("Sticky header"));
        affixCut.Find(".tento-affix").GetAttribute("style").Should().Contain("top: 20px;");
    }
}
