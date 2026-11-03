using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace TentoUI.Tests.DataEntry;

public class FormAndInputTests : TestContext
{
    private class TestModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string? Username { get; set; }

        public string? Description { get; set; }
    }

    [Theory]
    [InlineData(FormLayout.Vertical, "space-y-4")]
    [InlineData(FormLayout.Horizontal, "grid grid-cols-12 gap-4")]
    [InlineData(FormLayout.Inline, "flex flex-wrap items-center gap-4")]
    public void Form_Layouts_ApplyProperContainerClasses(FormLayout layout, string expectedClass)
    {
        var model = new TestModel();
        var cut = RenderComponent<Form>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Layout, layout)
            .AddChildContent("<div>Form Contents</div>"));

        var form = cut.Find("form");
        form.GetAttribute("class").Should().Contain(expectedClass);
    }

    [Fact]
    public void FormItem_RequiredFlag_RendersAsteriskWithAriaHidden()
    {
        var cut = RenderComponent<FormItem>(parameters => parameters
            .Add(p => p.FieldId, "email-field")
            .Add(p => p.Label, "Email Address")
            .Add(p => p.Required, true)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<Input>(0);
                builder.AddAttribute(1, "Value", "test@example.com");
                builder.CloseComponent();
            }));

        var label = cut.Find("label");
        label.GetAttribute("for").Should().Be("email-field");
        label.TextContent.Should().Contain("Email Address");

        var asterisk = cut.Find("span.text-red-500");
        asterisk.GetAttribute("aria-hidden").Should().Be("true");
        asterisk.TextContent.Should().Be("*");
    }

    [Fact]
    public void FormItem_WithErrorStatus_RendersRoleAlert()
    {
        var cut = RenderComponent<FormItem>(parameters => parameters
            .Add(p => p.FieldId, "password-field")
            .Add(p => p.Label, "Password")
            .Add(p => p.Error, "Password must be at least 8 characters")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<Input>(0);
                builder.CloseComponent();
            }));

        var alert = cut.Find("[role='alert']");
        alert.GetAttribute("id").Should().Be("password-field-error");
        alert.TextContent.Should().Contain("Password must be at least 8 characters");
    }

    [Fact]
    public void Input_TwoWayBinding_UpdatesValueOnInput()
    {
        string boundValue = "hello";
        var cut = RenderComponent<Input>(parameters => parameters
            .Add(p => p.Value, boundValue)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, v => boundValue = v)));

        var input = cut.Find("input");
        input.GetAttribute("value").Should().Be("hello");

        input.Input("world");
        boundValue.Should().Be("world");
    }

    [Fact]
    public void Input_AllowClear_ClearsValueOnClick()
    {
        string boundValue = "Clear me";
        var cut = RenderComponent<Input>(parameters => parameters
            .Add(p => p.Value, boundValue)
            .Add(p => p.AllowClear, true)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, v => boundValue = v)));

        var clearBtn = cut.Find(".tento-input-clear-btn");
        clearBtn.Click();

        boundValue.Should().BeEmpty();
    }

    [Fact]
    public void TextArea_RendersTextareaTagWithRows()
    {
        var cut = RenderComponent<TextArea>(parameters => parameters
            .Add(p => p.Rows, 5)
            .Add(p => p.Value, "Multiline text"));

        var textarea = cut.Find("textarea");
        textarea.GetAttribute("rows").Should().Be("5");
        textarea.TextContent.Should().Be("Multiline text");
    }

    [Fact]
    public void SearchInput_ClickButton_TriggersOnSearch()
    {
        string searchedTerm = string.Empty;
        var cut = RenderComponent<SearchInput>(parameters => parameters
            .Add(p => p.Value, "blazor components")
            .Add(p => p.OnSearch, EventCallback.Factory.Create<string?>(this, (string? s) => searchedTerm = s ?? string.Empty)));

        var searchBtn = cut.Find("button");
        searchBtn.Click();

        searchedTerm.Should().Be("blazor components");
    }

    [Fact]
    public void NumberInput_WithDecimalMinAndStep_RendersAndParsesCorrectly()
    {
        decimal price = 19.99m;
        var cut = RenderComponent<NumberInput<decimal>>(parameters => parameters
            .Add(p => p.Value, price)
            .Add(p => p.Min, 0.01m)
            .Add(p => p.Step, 0.05m)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<decimal>(this, v => price = v)));

        var input = cut.Find("input");
        input.GetAttribute("min").Should().Be("0.01");
        input.GetAttribute("step").Should().Be("0.05");
        input.GetAttribute("value").Should().Be("19.99");
    }

    [Fact]
    public void InputNumber_WithIntMinAndStep_RendersAndParsesCorrectly()
    {
        int quantity = 5;
        var cut = RenderComponent<InputNumber<int>>(parameters => parameters
            .Add(p => p.Value, quantity)
            .Add(p => p.Min, 1)
            .Add(p => p.Step, 1)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<int>(this, v => quantity = v)));

        var input = cut.Find("input");
        input.GetAttribute("min").Should().Be("1");
        input.GetAttribute("step").Should().Be("1");
        input.GetAttribute("value").Should().Be("5");
    }
}
