namespace TentoUI.Tests.Accessibility;

public class A11yContractTests : TestContext
{
    public A11yContractTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Modal_SatisfiesAriaContract()
    {
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.Title, "A11y Dialog")
            .AddChildContent("<p>Dialog body</p>"));

        var markup = cut.Markup;
        markup.Should().Contain("role=\"dialog\"");
        markup.Should().Contain("aria-modal=\"true\"");
        markup.Should().Contain("aria-labelledby");
        markup.Should().Contain("aria-describedby");
    }

    [Fact]
    public void Tabs_SatisfiesAriaContract()
    {
        var cut = RenderComponent<Tabs>(parameters => parameters
            .AddChildContent(builder =>
            {
                builder.OpenComponent<TabPane>(0);
                builder.AddAttribute(1, "Key", "tab1");
                builder.AddAttribute(2, "Tab", "First Tab");
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(4, "Tab 1 Content")));
                builder.CloseComponent();
            }));

        var markup = cut.Markup;
        markup.Should().Contain("role=\"tablist\"");
        markup.Should().Contain("role=\"tab\"");
        markup.Should().Contain("role=\"tabpanel\"");
        markup.Should().Contain("aria-selected");
        markup.Should().Contain("aria-controls");
    }

    [Fact]
    public void Menu_SatisfiesAriaContract()
    {
        var cut = RenderComponent<Menu>(parameters => parameters
            .AddChildContent(builder =>
            {
                builder.OpenComponent<MenuItem>(0);
                builder.AddAttribute(1, "Key", "m1");
                builder.AddAttribute(2, "Disabled", true);
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(4, "Disabled Item")));
                builder.CloseComponent();
            }));

        var markup = cut.Markup;
        markup.Should().Contain("role=\"menu\"");
        markup.Should().Contain("role=\"menuitem\"");
        markup.Should().Contain("aria-disabled");
    }

    [Fact]
    public void SubMenu_SatisfiesAriaContract()
    {
        var cut = RenderComponent<Menu>(parameters => parameters
            .AddChildContent(builder =>
            {
                builder.OpenComponent<SubMenu>(0);
                builder.AddAttribute(1, "Key", "sub");
                builder.AddAttribute(2, "Title", "Sub Navigation");
                builder.CloseComponent();
            }));

        var markup = cut.Markup;
        markup.Should().Contain("aria-haspopup=\"true\"");
        markup.Should().Contain("aria-expanded");
    }

    [Fact]
    public void FormItem_SatisfiesAriaContract()
    {
        var cut = RenderComponent<FormItem>(parameters => parameters
            .Add(p => p.FieldId, "username")
            .Add(p => p.Label, "Username")
            .Add(p => p.Error, "Username is required")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<Input>(0);
                builder.CloseComponent();
            }));

        var markup = cut.Markup;
        markup.Should().Contain("<label");
        markup.Should().Contain("for=");
        markup.Should().Contain("role=\"alert\"");
        markup.Should().Contain("aria-invalid");
        markup.Should().Contain("aria-describedby");
    }

    [Fact]
    public void Spin_SatisfiesAriaContract()
    {
        var cut = RenderComponent<Spin>(parameters => parameters
            .Add(p => p.Spinning, true));

        var markup = cut.Markup;
        markup.Should().Contain("role=\"status\"");
        markup.Should().Contain("aria-live=\"polite\"");
    }

    [Fact]
    public void Alert_SatisfiesAriaContract()
    {
        var alertCut = RenderComponent<Alert>(parameters => parameters
            .Add(p => p.Type, AlertType.Error)
            .Add(p => p.Message, "Critical Failure"));

        var statusCut = RenderComponent<Alert>(parameters => parameters
            .Add(p => p.Type, AlertType.Info)
            .Add(p => p.Message, "Informational note"));

        alertCut.Markup.Should().Contain("role=\"alert\"");
        statusCut.Markup.Should().Contain("role=\"status\"");
    }

    [Fact]
    public void Checkbox_SatisfiesAriaContract()
    {
        var cut = RenderComponent<Checkbox>(parameters => parameters
            .Add(p => p.Checked, true)
            .AddChildContent("Accept Terms"));

        var markup = cut.Markup;
        markup.Should().Contain("type=\"checkbox\"");
        markup.Should().Contain("aria-checked");
    }

    [Fact]
    public void Switch_SatisfiesAriaContract()
    {
        var cut = RenderComponent<Switch>(parameters => parameters
            .Add(p => p.Checked, true));

        var markup = cut.Markup;
        markup.Should().Contain("role=\"switch\"");
        markup.Should().Contain("aria-checked");
    }
}
