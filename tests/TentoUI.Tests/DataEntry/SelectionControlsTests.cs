namespace TentoUI.Tests.DataEntry;

public class SelectionControlsTests : TestContext
{
    public record OptionItem(int Id, string Name);

    [Fact]
    public void Select_SingleSelect_RendersAndSelectsOption()
    {
        var items = new System.Collections.Generic.List<OptionItem>
        {
            new(1, "Option One"),
            new(2, "Option Two")
        };

        int selectedId = 0;
        var cut = RenderComponent<Select<int, OptionItem>>(parameters => parameters
            .Add(p => p.DataSource, items)
            .Add(p => p.ValueProperty, x => x.Id)
            .Add(p => p.LabelProperty, x => x.Name)
            .Add(p => p.Value, selectedId)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<int>(this, v => selectedId = v)));

        // Click trigger to open dropdown
        var trigger = cut.Find("[role='combobox']");
        trigger.GetAttribute("aria-expanded").Should().Be("false");
        trigger.Click();
        trigger.GetAttribute("aria-expanded").Should().Be("true");

        // Options are rendered
        var options = cut.FindAll("[role='option']");
        options.Count.Should().Be(2);

        // Click first option
        options[0].Click();
        selectedId.Should().Be(1);
    }

    [Fact]
    public void Checkbox_TwoWayBinding_TogglesChecked()
    {
        bool isChecked = false;
        var cut = RenderComponent<Checkbox>(parameters => parameters
            .Add(p => p.Checked, isChecked)
            .Add(p => p.CheckedChanged, EventCallback.Factory.Create<bool>(this, v => isChecked = v))
            .AddChildContent("Remember me"));

        var input = cut.Find("input[type='checkbox']");
        input.GetAttribute("aria-checked").Should().Be("false");

        input.Change(true);
        isChecked.Should().BeTrue();
    }

    [Fact]
    public void CheckboxGroup_SelectsMultipleOptions()
    {
        var options = new System.Collections.Generic.List<CheckboxOption<string>>
        {
            new("A", "Apple"),
            new("B", "Banana")
        };

        System.Collections.Generic.IEnumerable<string> values = new[] { "A" };
        var cut = RenderComponent<CheckboxGroup<string>>(parameters => parameters
            .Add(p => p.Options, options)
            .Add(p => p.Values, values)
            .Add(p => p.ValuesChanged, EventCallback.Factory.Create<System.Collections.Generic.IEnumerable<string>>(this, v => values = v)));

        var inputs = cut.FindAll("input[type='checkbox']");
        inputs.Count.Should().Be(2);
        inputs[0].GetAttribute("aria-checked").Should().Be("true");
        inputs[1].GetAttribute("aria-checked").Should().Be("false");

        // Toggle second
        inputs[1].Change(true);
        values.Should().Contain("B");
    }

    [Fact]
    public void RadioGroup_SelectsSingleOption()
    {
        string selectedValue = "1";
        var cut = RenderComponent<RadioGroup<string>>(parameters => parameters
            .Add(p => p.Value, selectedValue)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, v => selectedValue = v))
            .AddChildContent(builder =>
            {
                builder.OpenComponent<Radio<string>>(0);
                builder.AddAttribute(1, "Value", "1");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(3, "First")));
                builder.CloseComponent();

                builder.OpenComponent<Radio<string>>(4);
                builder.AddAttribute(5, "Value", "2");
                builder.AddAttribute(6, "ChildContent", (RenderFragment)(b => b.AddContent(7, "Second")));
                builder.CloseComponent();
            }));

        var radios = cut.FindAll("input[type='radio']");
        radios.Count.Should().Be(2);

        // Select second radio
        radios[1].Change("2");
        selectedValue.Should().Be("2");
    }

    [Fact]
    public void Switch_RendersRoleSwitchAndTogglesState()
    {
        bool isSwitched = false;
        var cut = RenderComponent<Switch>(parameters => parameters
            .Add(p => p.Checked, isSwitched)
            .Add(p => p.CheckedChanged, EventCallback.Factory.Create<bool>(this, v => isSwitched = v)));

        var btn = cut.Find("button");
        btn.GetAttribute("role").Should().Be("switch");
        btn.GetAttribute("aria-checked").Should().Be("false");

        btn.Click();
        isSwitched.Should().BeTrue();
    }

    [Fact]
    public void DatePicker_RendersDateInput()
    {
        DateTime? date = new DateTime(2026, 4, 17);
        var cut = RenderComponent<DatePicker>(parameters => parameters
            .Add(p => p.Value, date));

        var input = cut.Find("input");
        input.GetAttribute("value").Should().Be("2026-04-17");
    }
}
