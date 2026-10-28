using Microsoft.Extensions.DependencyInjection;

namespace TentoUI.Tests.Components;

public class HighValueComponentsTests : TestContext
{
    [Fact]
    public void NotificationService_And_NotificationHost_RenderAndClose()
    {
        var service = new NotificationService();
        Services.AddSingleton<INotificationService>(service);

        var cut = RenderComponent<NotificationHost>();

        var notif = service.Success("Deployment Succeeded", "All 5 nodes upgraded.");
        cut.Render();

        cut.Find("#tento-notification-" + notif.Key).Should().NotBeNull();
        cut.Markup.Should().Contain("Deployment Succeeded");
        cut.Markup.Should().Contain("All 5 nodes upgraded.");

        cut.Find("button[aria-label='Close notification']").Click();
        cut.Render();
        cut.FindAll("#tento-notification-" + notif.Key).Should().BeEmpty();
    }

    [Fact]
    public void Segmented_SelectionAndOptionDisabling()
    {
        string selected = "daily";
        var options = new[]
        {
            new SegmentedOption<string>("daily", "Daily"),
            new SegmentedOption<string>("weekly", "Weekly"),
            new SegmentedOption<string>("monthly", "Monthly", Disabled: true)
        };

        var cut = RenderComponent<Segmented<string>>(parameters => parameters
            .Add(p => p.Value, selected)
            .Add(p => p.Options, options)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, val => selected = val)));

        var buttons = cut.FindAll("button[role='radio']");
        buttons.Count.Should().Be(3);
        buttons[0].GetAttribute("aria-checked").Should().Be("true");

        // Click disabled option -> should not change
        cut.Find("button[data-value='monthly']").Click();
        selected.Should().Be("daily");

        // Click weekly option
        cut.Find("button[data-value='weekly']").Click();
        selected.Should().Be("weekly");
    }

    [Fact]
    public void AutoComplete_FilterSuggestionsAndSelect()
    {
        string selectedText = string.Empty;
        var cut = RenderComponent<AutoComplete>(parameters => parameters
            .Add(p => p.SimpleOptions, new[] { "Apple", "Banana", "Apricot", "Berry" })
            .Add(p => p.OnSelect, EventCallback.Factory.Create<string>(this, val => selectedText = val)));

        var input = cut.Find("input");
        input.Input("Ap");

        var options = cut.FindAll("li[role='option']");
        options.Count.Should().Be(2); // Apple and Apricot

        options[1].Click();
        selectedText.Should().Be("Apricot");
    }

    [Fact]
    public void TimePicker_PopupAndSelection()
    {
        TimeOnly? selectedTime = null;
        var cut = RenderComponent<TimePicker>(parameters => parameters
            .Add(p => p.Value, selectedTime)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<TimeOnly?>(this, t => selectedTime = t)));

        cut.FindAll(".tento-timepicker-popup").Should().BeEmpty();

        // Click input to open popup
        cut.Find("input").Click();
        cut.Find(".tento-timepicker-popup").Should().NotBeNull();

        // Click "Now"
        cut.Find("button:contains('Now')").Click();
        selectedTime.Should().NotBeNull();
    }

    [Fact]
    public void Carousel_PanelsAndNavigation()
    {
        int currentIndex = 0;
        var cut = RenderComponent<Carousel>(parameters => parameters
            .Add(p => p.CurrentIndex, currentIndex)
            .Add(p => p.Arrows, true)
            .Add(p => p.CurrentIndexChanged, EventCallback.Factory.Create<int>(this, idx => currentIndex = idx))
            .AddChildContent(builder =>
            {
                builder.OpenComponent<CarouselPanel>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Slide 1 Content")));
                builder.CloseComponent();

                builder.OpenComponent<CarouselPanel>(2);
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Slide 2 Content")));
                builder.CloseComponent();
            }));

        cut.Markup.Should().Contain("Slide 1 Content");

        // Click next arrow
        cut.Find("button[aria-label='Next slide']").Click();
        currentIndex.Should().Be(1);
    }

    [Fact]
    public void Image_PreviewModal_OpensAndCloses()
    {
        var cut = RenderComponent<Image>(parameters => parameters
            .Add(p => p.Src, "https://example.com/photo.jpg")
            .Add(p => p.Preview, true));

        cut.FindAll(".tento-image-preview-root").Should().BeEmpty();

        // Click preview trigger
        cut.Find(".group > div").Click();
        cut.Find(".tento-image-preview-root").Should().NotBeNull();

        // Click close preview button
        cut.Find("button[aria-label='Close preview']").Click();
        cut.FindAll(".tento-image-preview-root").Should().BeEmpty();
    }

    [Fact]
    public void FloatButton_And_FloatButtonGroup_RenderProperly()
    {
        bool clicked = false;
        var cut = RenderComponent<FloatButtonGroup>(p => p.AddChildContent(b =>
        {
            b.OpenComponent<FloatButton>(0);
            b.AddAttribute(1, "Icon", IconNames.HelpCircle);
            b.AddAttribute(2, "Description", "Help");
            b.AddAttribute(3, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicked = true));
            b.CloseComponent();
        }));

        cut.Find(".tento-float-button").Click();
        clicked.Should().BeTrue();
    }

    [Fact]
    public void ColorPicker_SwatchAndPresetSelection()
    {
        string color = "#1677ff";
        var cut = RenderComponent<ColorPicker>(parameters => parameters
            .Add(p => p.Value, color)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, c => color = c)));

        // Open popup
        cut.Find("button[aria-label='Pick color']").Click();
        cut.Find(".tento-colorpicker-popup").Should().NotBeNull();

        // Click preset color button
        cut.Find("button[title='#52c41a']").Click();
        color.Should().Be("#52c41a");
    }
}
