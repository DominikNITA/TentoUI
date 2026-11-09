namespace TentoUI.Tests.Navigation;

public class NavigationTests : TestContext
{
    [Fact]
    public void Menu_RendersMenuRoleAndItems()
    {
        var cut = RenderComponent<Menu>(parameters => parameters
            .Add(p => p.Mode, MenuMode.Inline)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<MenuItem>(0);
                builder.AddAttribute(1, "Key", "home");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(3, "Home")));
                builder.CloseComponent();
            }));

        var menu = cut.Find("nav");
        menu.GetAttribute("role").Should().Be("menu");

        var item = cut.Find("[role='menuitem']");
        item.TextContent.Should().Contain("Home");
    }

    [Fact]
    public void MenuItem_Click_SelectsKeyAndInvokesCallback()
    {
        string? selectedKey = null;
        var cut = RenderComponent<Menu>(parameters => parameters
            .Add(p => p.SelectedKey, "initial")
            .Add(p => p.SelectedKeyChanged, EventCallback.Factory.Create<string>(this, k => selectedKey = k))
            .AddChildContent(builder =>
            {
                builder.OpenComponent<MenuItem>(0);
                builder.AddAttribute(1, "Key", "dashboard");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(3, "Dashboard")));
                builder.CloseComponent();
            }));

        var item = cut.Find("[role='menuitem']");
        item.Click();

        selectedKey.Should().Be("dashboard");
    }

    [Fact]
    public void SubMenu_RendersAriaHasPopupAndExpanded()
    {
        var cut = RenderComponent<Menu>(parameters => parameters
            .AddChildContent(builder =>
            {
                builder.OpenComponent<SubMenu>(0);
                builder.AddAttribute(1, "Key", "sub1");
                builder.AddAttribute(2, "Title", "Settings");
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
                {
                    b.OpenComponent<MenuItem>(4);
                    b.AddAttribute(5, "Key", "sub1-1");
                    b.AddAttribute(6, "ChildContent", (RenderFragment)(b2 => b2.AddContent(7, "Option 1")));
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            }));

        var trigger = cut.Find("[aria-haspopup='true']");
        trigger.Should().NotBeNull();
        trigger.GetAttribute("aria-expanded").Should().Be("false");

        trigger.Click();
        cut.Find("[aria-haspopup='true']").GetAttribute("aria-expanded").Should().Be("true");
    }

    [Fact]
    public void Breadcrumb_RendersItemsAndSeparators()
    {
        var cut = RenderComponent<Breadcrumb>(parameters => parameters
            .AddChildContent(builder =>
            {
                builder.OpenComponent<BreadcrumbItem>(0);
                builder.AddAttribute(1, "Href", "/");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(3, "Home")));
                builder.CloseComponent();

                builder.OpenComponent<BreadcrumbItem>(4);
                builder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(6, "Library")));
                builder.CloseComponent();
            }));

        var nav = cut.Find("nav");
        nav.GetAttribute("aria-label").Should().Be("Breadcrumb");
        cut.FindAll("li").Count.Should().Be(2);
    }

    [Fact]
    public void Tabs_RendersTablistAndTabPanesWithRovingSelection()
    {
        var cut = RenderComponent<Tabs>(parameters => parameters
            .Add(p => p.ActiveKey, "1")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<TabPane>(0);
                builder.AddAttribute(1, "Key", "1");
                builder.AddAttribute(2, "Tab", "Tab One");
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(4, "Tab 1 Content")));
                builder.CloseComponent();

                builder.OpenComponent<TabPane>(5);
                builder.AddAttribute(6, "Key", "2");
                builder.AddAttribute(7, "Tab", "Tab Two");
                builder.AddAttribute(8, "ChildContent", (RenderFragment)(b => b.AddContent(9, "Tab 2 Content")));
                builder.CloseComponent();
            }));

        var tablist = cut.Find("[role='tablist']");
        tablist.Should().NotBeNull();

        var tabs = cut.FindAll("[role='tab']");
        tabs.Count.Should().Be(2);
        tabs[0].GetAttribute("aria-selected").Should().Be("true");
        tabs[1].GetAttribute("aria-selected").Should().Be("false");

        // Click second tab
        tabs[1].Click();

        cut.FindAll("[role='tab']")[1].GetAttribute("aria-selected").Should().Be("true");
    }

    [Fact]
    public void Tabs_MultipleRendersWithDynamicContent_DoesNotCauseInfiniteLoop()
    {
        var counter = 0;
        var cut = RenderComponent<Tabs>(parameters => parameters
            .Add(p => p.ActiveKey, "1")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<TabPane>(0);
                builder.AddAttribute(1, "Key", "1");
                builder.AddAttribute(2, "Tab", "Tab One");
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(4, $"Render count: {++counter}")));
                builder.CloseComponent();

                builder.OpenComponent<TabPane>(5);
                builder.AddAttribute(6, "Key", "2");
                builder.AddAttribute(7, "Tab", "Tab Two");
                builder.AddAttribute(8, "ChildContent", (RenderFragment)(b => b.AddContent(9, "Static content")));
                builder.CloseComponent();
            }));

        // Re-render multiple times
        for (var i = 0; i < 5; i++)
        {
            cut.SetParametersAndRender(parameters => parameters
                .Add(p => p.ActiveKey, i % 2 == 0 ? "1" : "2"));
        }

        cut.FindAll("[role='tab']").Should().HaveCount(2);
    }

    [Fact]
    public void CommandPalette_WhenOpen_RendersDialogWithAutofocusInput()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<CommandItem>
        {
            new("btn", "Button", "Components", "Buttons documentation", IconNames.Search, "B"),
            new("inp", "Input", "Components", "Input documentation", IconNames.Edit, "I")
        };

        var cut = RenderComponent<CommandPalette>(parameters => parameters
            .Add(p => p.Open, true)
            .Add(p => p.Items, items));

        var dialog = cut.Find("[role='dialog']");
        dialog.Should().NotBeNull();

        var input = cut.Find("input.tento-command-palette-input");
        input.Should().NotBeNull();
        input.HasAttribute("autofocus").Should().BeTrue();

        var renderedItems = cut.FindAll("[role='option']");
        renderedItems.Should().HaveCount(2);
        renderedItems[0].TextContent.Should().Contain("Button");
        renderedItems[1].TextContent.Should().Contain("Input");
    }

    [Fact]
    public void CommandPalette_FilterQuery_FiltersDisplayedItems()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<CommandItem>
        {
            new("btn", "Button", "Components", "Buttons documentation"),
            new("inp", "Input", "Components", "Input documentation")
        };

        var cut = RenderComponent<CommandPalette>(parameters => parameters
            .Add(p => p.Open, true)
            .Add(p => p.Items, items));

        var input = cut.Find("input.tento-command-palette-input");
        input.Input("input");

        var renderedItems = cut.FindAll("[role='option']");
        renderedItems.Should().HaveCount(1);
        renderedItems[0].TextContent.Should().Contain("Input");
    }
}
