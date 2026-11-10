using System;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TentoUI;
using Xunit;

namespace TentoUI.Tests.Components;

public class AuditRemediationTests : TestContext
{
    public AuditRemediationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void AddTentoUI_WithDirectThemeInstance_RegistersServices()
    {
        var services = new ServiceCollection();
        var customTheme = new TentoTheme
        {
            PrimaryColor = "#2563eb",
            BorderRadius = "8px"
        };

        services.AddTentoUI(customTheme);
        var provider = services.BuildServiceProvider();

        var resolvedTheme = provider.GetRequiredService<TentoTheme>();
        resolvedTheme.Should().BeSameAs(customTheme);
        resolvedTheme.PrimaryColor.Should().Be("#2563eb");
        provider.GetRequiredService<IModalService>().Should().NotBeNull();
        provider.GetRequiredService<IMessageService>().Should().NotBeNull();
        provider.GetRequiredService<INotificationService>().Should().NotBeNull();
    }

    [Fact]
    public void Form_WithoutModelOrEditContext_RendersGracefullyWithoutCrashing()
    {
        // Must not throw InvalidOperationException: EditForm requires either a Model parameter, or an EditContext parameter
        Action act = () => RenderComponent<Form>(p => p
            .AddChildContent("<div id='test-content'>Form Inner Content</div>"));

        act.Should().NotThrow();
    }

    [Fact]
    public void StepperForm_WithoutModel_RendersGracefullyWithoutCrashing()
    {
        // Must not throw InvalidOperationException
        Action act = () => RenderComponent<StepperForm<object>>(p => p
            .AddChildContent(builder =>
            {
                builder.OpenComponent<StepperFormStep>(0);
                builder.AddAttribute(1, "Title", "Step 1");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(3, "Step 1 content")));
                builder.CloseComponent();
            }));

        act.Should().NotThrow();
    }

    [Fact]
    public void Switch_A11y_RendersAccessibleName()
    {
        // 1. Fallback accessible name
        var cut1 = RenderComponent<Switch>();
        var button1 = cut1.Find("button[role='switch']");
        button1.GetAttribute("aria-label").Should().Be("Toggle switch");

        // 2. Custom AriaLabel parameter
        var cut2 = RenderComponent<Switch>(p => p.Add(x => x.AriaLabel, "Enable dark mode"));
        var button2 = cut2.Find("button[role='switch']");
        button2.GetAttribute("aria-label").Should().Be("Enable dark mode");
    }

    [Fact]
    public void Switch_InsideFormItem_InheritsLabelForAria()
    {
        var cut = RenderComponent<FormItem>(p => p
            .Add(x => x.Label, "Push Notifications")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<Switch>(0);
                builder.CloseComponent();
            }));

        var switchBtn = cut.Find("button[role='switch']");
        switchBtn.GetAttribute("aria-label").Should().Be("Push Notifications");
    }

    [Fact]
    public void Progress_And_Gauge_RenderAccessibleNames()
    {
        // Progress
        var progCut = RenderComponent<Progress>(p => p.Add(x => x.Percent, 75));
        progCut.Find("div[role='progressbar']").GetAttribute("aria-label").Should().Be("Progress");

        var customProgCut = RenderComponent<Progress>(p => p
            .Add(x => x.Percent, 40)
            .Add(x => x.AriaLabel, "Upload progress"));
        customProgCut.Find("div[role='progressbar']").GetAttribute("aria-label").Should().Be("Upload progress");

        // Gauge
        var gaugeCut = RenderComponent<Gauge>(p => p
            .Add(x => x.Value, 82)
            .Add(x => x.AriaLabel, "CPU utilization"));
        gaugeCut.Find("div[role='meter']").GetAttribute("aria-label").Should().Be("CPU utilization");
    }

    [Fact]
    public void Carousel_Dots_AreWrappedInTablist()
    {
        var cut = RenderComponent<Carousel>(p => p
            .Add(x => x.Dots, true)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<CarouselPanel>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(2, "Slide 1")));
                builder.CloseComponent();

                builder.OpenComponent<CarouselPanel>(3);
                builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(5, "Slide 2")));
                builder.CloseComponent();
            }));

        var tablist = cut.Find("div[role='tablist']");
        tablist.Should().NotBeNull();
        tablist.GetAttribute("aria-label").Should().NotBeNullOrEmpty();
        var tabs = tablist.QuerySelectorAll("button[role='tab']");
        tabs.Should().HaveCount(2);
    }

    [Fact]
    public void Splitter_DoesNotNestInteractiveButtonsInsideSeparator()
    {
        var cut = RenderComponent<Splitter>(p => p
            .Add(x => x.Collapsible, true)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<SplitterPanel>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(2, "Left")));
                builder.CloseComponent();

                builder.OpenComponent<SplitterPanel>(3);
                builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(5, "Right")));
                builder.CloseComponent();
            }));

        var separator = cut.Find("div[role='separator']");
        // Separator must NOT contain any button elements (axe-core nested-interactive violation)
        separator.QuerySelectorAll("button").Should().BeEmpty();

        // The collapse button should exist as a sibling inside the splitter bar container
        cut.Find("button.tento-splitter-collapse-btn").Should().NotBeNull();
    }

    [Fact]
    public void Badge_SupportsBadgeStatusEnum_AndCustomText()
    {
        var cut = RenderComponent<Badge>(p => p
            .Add(x => x.Status, BadgeStatus.Success)
            .Add(x => x.Text, "Active Deployment"));

        cut.Markup.Should().Contain("bg-emerald-500");
        cut.Markup.Should().Contain("Active Deployment");

        var processingCut = RenderComponent<Badge>(p => p
            .Add(x => x.Status, "BadgeStatus.Processing")
            .Add(x => x.Text, "Building"));

        processingCut.Markup.Should().Contain("bg-blue-500");
        processingCut.Markup.Should().Contain("Building");
    }

    [Fact]
    public void Rate_SupportsDoubleAndHalfStars()
    {
        double rating = 3.5;
        var cut = RenderComponent<Rate>(p => p
            .Add(x => x.Value, rating)
            .Add(x => x.AllowHalf, true)
            .Add(x => x.Count, 5)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<double>(this, v => rating = v)));

        // Star 4 should render half star
        cut.Find("button[data-star-index='4'] svg").Should().NotBeNull();
        cut.Markup.Should().Contain(IconNames.StarHalf);

        // Click on star 5
        cut.Find("button[data-star-index='5']").Click();
        rating.Should().Be(5);
    }

    [Fact]
    public void Statistic_SupportsPrefixText_And_SuffixText()
    {
        var cut = RenderComponent<Statistic>(p => p
            .Add(x => x.Title, "Revenue")
            .Add(x => x.Value, 12500)
            .Add(x => x.PrefixText, "$")
            .Add(x => x.SuffixText, "USD"));

        cut.Find(".tento-statistic-prefix").TextContent.Should().Be("$");
        cut.Find(".tento-statistic-suffix").TextContent.Should().Be("USD");
        cut.Find(".tento-statistic-value").TextContent.Should().Be("12500");
    }

    [Fact]
    public void NumberInput_AliasInstantiatesSuccessfully()
    {
        double val = 42;
        var cut = RenderComponent<NumberInput<double>>(p => p
            .Add(x => x.Value, val)
            .Add(x => x.Min, 0.0)
            .Add(x => x.Max, 100.0)
            .Add(x => x.Step, 2.0));

        var input = cut.Find("input[type='number']");
        input.GetAttribute("min").Should().Be("0");
        input.GetAttribute("max").Should().Be("100");
        input.GetAttribute("step").Should().Be("2");
    }

    [Fact]
    public void TentoConfigProvider_SupportsInheritFontFamily_AndSyncBodyTheme()
    {
        var cut = RenderComponent<TentoConfigProvider>(p => p
            .Add(x => x.InheritFontFamily, true)
            .Add(x => x.DarkMode, true)
            .AddChildContent("<p>Themed App</p>"));

        var root = cut.Find(".tento-config-provider");
        root.ClassList.Should().NotContain("font-sans");
        root.ClassList.Should().Contain("dark");
    }

    [Fact]
    public void Modal_FocusTrap_InvokesDirectJSInterop()
    {
        var plannedOpen = JSInterop.SetupVoid("tentoModal.open", _ => true);
        var plannedClose = JSInterop.SetupVoid("tentoModal.close", _ => true);

        var cut = RenderComponent<Modal>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.Title, "Accessible Modal")
            .AddChildContent("<p>Modal content</p>"));

        // Open should be invoked directly on JSRuntime
        plannedOpen.Invocations.Should().HaveCount(1);

        // Hide modal
        cut.SetParametersAndRender(p => p.Add(x => x.Visible, false));
        plannedClose.Invocations.Should().HaveCount(1);
    }
}

