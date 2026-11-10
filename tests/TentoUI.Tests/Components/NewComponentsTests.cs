namespace TentoUI.Tests.Components;

public class NewComponentsTests : TestContext
{
    [Fact]
    public void Pagination_Standalone_RendersEllipsisSizeChangerAndJumper()
    {
        int currentPage = 5;
        int currentSize = 10;

        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.TotalItems, 200)
            .Add(p => p.PageIndex, currentPage)
            .Add(p => p.PageSize, currentSize)
            .Add(p => p.ShowSizeChanger, true)
            .Add(p => p.ShowQuickJumper, true)
            .Add(p => p.PageIndexChanged, EventCallback.Factory.Create<int>(this, idx => currentPage = idx))
            .Add(p => p.PageSizeChanged, EventCallback.Factory.Create<int>(this, sz => currentSize = sz)));

        cut.Find(".tento-pagination-total").TextContent.Should().Contain("Showing 41 to 50 of 200");
        cut.Find("button[aria-current='page']").TextContent.Trim().Should().Be("5");

        // Jump to page 8
        cut.Find("input[aria-label='Jump to page']").Change("8");
        currentPage.Should().Be(8);

        // Change size to 50
        cut.Find("select[aria-label='Items per page']").Change("50");
        currentSize.Should().Be(50);
    }

    [Fact]
    public void Steps_RendersStepStatusesAndInvokesOnChange()
    {
        int activeStep = 1;

        var cut = RenderComponent<Steps>(parameters => parameters
            .Add(p => p.Current, activeStep)
            .Add(p => p.OnChange, EventCallback.Factory.Create<int>(this, s => activeStep = s))
            .AddChildContent(builder =>
            {
                builder.OpenComponent<Step>(0);
                builder.AddAttribute(1, "Title", "Finished Step");
                builder.CloseComponent();

                builder.OpenComponent<Step>(2);
                builder.AddAttribute(3, "Title", "In Progress");
                builder.CloseComponent();

                builder.OpenComponent<Step>(4);
                builder.AddAttribute(5, "Title", "Waiting");
                builder.CloseComponent();
            }));

        var items = cut.FindAll(".tento-step-item");
        items.Count.Should().Be(3);
        items[0].GetAttribute("data-step-status").Should().Be("finish");
        items[1].GetAttribute("data-step-status").Should().Be("process");
        items[2].GetAttribute("data-step-status").Should().Be("wait");

        items[2].Click();
        activeStep.Should().Be(2);
    }

    [Fact]
    public void Dropdown_ClickTrigger_OpensMenuAndSelectsItem()
    {
        string? clickedKey = null;
        var items = new[]
        {
            new DropdownMenuItem("profile", "My Profile", IconNames.User),
            new DropdownMenuItem("delete", "Delete Account", IconNames.Trash2, Danger: true, DividerBefore: true)
        };

        var cut = RenderComponent<Dropdown>(parameters => parameters
            .Add(p => p.Text, "Actions")
            .Add(p => p.Items, items)
            .Add(p => p.OnItemClick, EventCallback.Factory.Create<string>(this, k => clickedKey = k)));

        cut.FindAll(".tento-dropdown-menu").Should().BeEmpty();

        cut.Find(".tento-dropdown-trigger").Click();
        cut.Find(".tento-dropdown-menu").Should().NotBeNull();

        cut.Find("button[data-menu-key='delete']").Click();
        clickedKey.Should().Be("delete");
        cut.FindAll(".tento-dropdown-menu").Should().BeEmpty();
    }

    [Fact]
    public void Avatar_And_AvatarGroup_RenderCorrectly()
    {
        var cut = RenderComponent<AvatarGroup>(parameters => parameters
            .AddChildContent(builder =>
            {
                builder.OpenComponent<Avatar>(0);
                builder.AddAttribute(1, "Text", "JD");
                builder.AddAttribute(2, "Shape", AvatarShape.Square);
                builder.CloseComponent();

                builder.OpenComponent<Avatar>(3);
                builder.AddAttribute(4, "Icon", IconNames.User);
                builder.CloseComponent();
            }));

        var avatars = cut.FindAll(".tento-avatar");
        avatars.Count.Should().Be(2);
        avatars[0].TextContent.Should().Contain("JD");
        avatars[0].ClassList.Should().Contain("rounded-md");
    }

    [Fact]
    public void Collapse_AccordionMode_TogglesPanelsExclusively()
    {
        var cut = RenderComponent<Collapse>(parameters => parameters
            .Add(p => p.Accordion, true)
            .Add(p => p.DefaultActiveKey, "p1")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<CollapsePanel>(0);
                builder.AddAttribute(1, "Key", "p1");
                builder.AddAttribute(2, "Header", "Panel 1");
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                builder.CloseComponent();

                builder.OpenComponent<CollapsePanel>(4);
                builder.AddAttribute(5, "Key", "p2");
                builder.AddAttribute(6, "Header", "Panel 2");
                builder.AddAttribute(7, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
                builder.CloseComponent();
            }));

        cut.Markup.Should().Contain("Content 1");
        cut.Markup.Should().NotContain("Content 2");

        var headers = cut.FindAll(".tento-collapse-header");
        headers[1].Click();

        cut.Markup.Should().NotContain("Content 1");
        cut.Markup.Should().Contain("Content 2");
    }

    [Fact]
    public void Empty_Descriptions_Statistic_Timeline_Popover_RenderProperly()
    {
        var emptyCut = RenderComponent<Empty>(p => p.Add(x => x.Description, "Nothing found"));
        emptyCut.Find(".tento-empty-description").TextContent.Should().Be("Nothing found");

        var statCut = RenderComponent<Statistic>(p => p
            .Add(x => x.Title, "Active Users")
            .Add(x => x.Value, 12345.678)
            .Add(x => x.Precision, 2));
        statCut.Find(".tento-statistic-value").TextContent.Should().Be("12,345.68");

        var descCut = RenderComponent<Descriptions>(p => p
            .Add(x => x.Title, "User Info")
            .Add(x => x.Bordered, true)
            .AddChildContent(b =>
            {
                b.OpenComponent<DescriptionsItem>(0);
                b.AddAttribute(1, "Label", "Username");
                b.AddAttribute(2, "ChildContent", (RenderFragment)(cb => cb.AddContent(0, "dominik")));
                b.CloseComponent();
            }));
        descCut.Find(".tento-descriptions-label").TextContent.Should().Contain("Username");
        descCut.Find(".tento-descriptions-content").TextContent.Should().Contain("dominik");

        var timelineCut = RenderComponent<Timeline>(p => p.AddChildContent(b =>
        {
            b.OpenComponent<TimelineItem>(0);
            b.AddAttribute(1, "Color", "green");
            b.AddAttribute(2, "Label", "2026-10-28");
            b.AddAttribute(3, "ChildContent", (RenderFragment)(cb => cb.AddContent(0, "Deployed release")));
            b.CloseComponent();
        }));
        timelineCut.Find(".tento-timeline-content").TextContent.Should().Be("Deployed release");

        var popoverCut = RenderComponent<Popover>(p => p
            .Add(x => x.Title, "Help")
            .Add(x => x.Content, "Detailed explanation")
            .AddChildContent(b => b.AddMarkupContent(0, "<button>Info</button>")));
        popoverCut.Find(".tento-popover-trigger").Click();
        popoverCut.Find(".tento-popover-title").TextContent.Should().Be("Help");
        popoverCut.Find(".tento-popover-content").TextContent.Should().Be("Detailed explanation");
    }

    [Fact]
    public void DataEntry_InputNumber_Slider_Rate_Upload_WorkCorrectly()
    {
        int numVal = 10;
        var numCut = RenderComponent<InputNumber<int>>(p => p
            .Add(x => x.Value, numVal)
            .Add(x => x.Min, 0)
            .Add(x => x.Max, 15)
            .Add(x => x.Step, 5)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<int>(this, v => numVal = v)));
        numCut.Find("button[aria-label='Increase Value']").Click();
        numVal.Should().Be(15);

        double sliderVal = 25;
        var sliderCut = RenderComponent<Slider>(p => p
            .Add(x => x.Value, sliderVal)
            .Add(x => x.ShowValue, true)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<double>(this, v => sliderVal = v)));
        sliderCut.Find("input[type='range']").Change("60");
        sliderVal.Should().Be(60);

        double rating = 3;
        var rateCut = RenderComponent<Rate>(p => p
            .Add(x => x.Value, rating)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<double>(this, v => rating = v)));
        rateCut.Find("button[data-star-index='5']").Click();
        rating.Should().Be(5);

        var files = new System.Collections.Generic.List<UploadFileItem>
        {
            new() { Name = "report.pdf", Size = 1024 }
        };
        var uploadCut = RenderComponent<Upload>(p => p
            .Add(x => x.DragDrop, true)
            .Add(x => x.FileList, files));
        uploadCut.Find(".tento-upload-list-item").TextContent.Should().Contain("report.pdf");
        uploadCut.Find("button[aria-label='Remove report.pdf']").Click();
        files.Should().BeEmpty();
    }

    [Fact]
    public void Feedback_Drawer_Progress_Skeleton_Popconfirm_Result_WorkCorrectly()
    {
        bool drawerOpen = true;
        var drawerCut = RenderComponent<Drawer>(p => p
            .Add(x => x.Open, drawerOpen)
            .Add(x => x.Title, "Settings Drawer")
            .Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => drawerOpen = v))
            .AddChildContent("Drawer body text"));
        drawerCut.Find(".tento-drawer-title").TextContent.Should().Be("Settings Drawer");
        drawerCut.Find("button[aria-label='Close drawer']").Click();
        drawerOpen.Should().BeFalse();

        var progressCut = RenderComponent<Progress>(p => p
            .Add(x => x.Percent, 100));
        progressCut.Find("[role='progressbar']").GetAttribute("data-progress-status").Should().Be("success");

        var skeletonCut = RenderComponent<Skeleton>(p => p
            .Add(x => x.Loading, true)
            .Add(x => x.Avatar, true)
            .Add(x => x.Rows, 2));
        skeletonCut.FindAll(".tento-skeleton-line").Count.Should().Be(2);

        bool confirmed = false;
        var popconfirmCut = RenderComponent<Popconfirm>(p => p
            .Add(x => x.Title, "Delete record?")
            .Add(x => x.OnConfirm, EventCallback.Factory.Create(this, () => confirmed = true))
            .AddChildContent("<button>Delete</button>"));
        popconfirmCut.Find(".tento-popconfirm-trigger").Click();
        popconfirmCut.Find(".tento-popconfirm-ok").Click();
        confirmed.Should().BeTrue();

        var resultCut = RenderComponent<Result>(p => p
            .Add(x => x.Status, ResultStatus.Success)
            .Add(x => x.Title, "Payment Completed")
            .Add(x => x.SubTitle, "Order #1024 processed."));
        resultCut.Find(".tento-result-title").TextContent.Should().Be("Payment Completed");
    }
}
