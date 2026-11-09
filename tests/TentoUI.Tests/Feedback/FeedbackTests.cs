using Microsoft.Extensions.DependencyInjection;

namespace TentoUI.Tests.Feedback;

public class FeedbackTests : TestContext
{
    public FeedbackTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTentoUI();
    }

    [Fact]
    public void Modal_WhenVisible_RendersBackdropAndDialog()
    {
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.Title, "Confirm Action")
            .AddChildContent("Modal Body Content"));

        var dialog = cut.Find("[role='dialog']");
        dialog.GetAttribute("aria-modal").Should().Be("true");
        cut.Find(".tento-modal-backdrop").Should().NotBeNull();
        dialog.TextContent.Should().Contain("Confirm Action");
        dialog.TextContent.Should().Contain("Modal Body Content");
    }

    [Fact]
    public void Modal_WhenInvisible_DoesNotRender()
    {
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, false)
            .AddChildContent("Invisible Body"));

        cut.FindAll("[role='dialog']").Should().BeEmpty();
    }

    [Fact]
    public void Modal_Closable_RendersCloseButtonWithEscapeDismiss()
    {
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.Closable, true));

        var closeBtn = cut.Find(".tento-modal-close");
        closeBtn.GetAttribute("aria-label").Should().Be("Close dialog");
        closeBtn.GetAttribute("data-dismiss-key").Should().Be("Escape");
    }

    [Fact]
    public void Modal_Footer_RendersOkAndCancelButtons()
    {
        bool okClicked = false;
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.OkText, "Save Changes")
            .Add(p => p.CancelText, "Discard")
            .Add(p => p.OnOk, () => okClicked = true));

        var buttons = cut.FindAll("button");
        buttons.Any(b => b.TextContent.Contains("Save Changes")).Should().BeTrue();
        buttons.Any(b => b.TextContent.Contains("Discard")).Should().BeTrue();

        var okBtn = buttons.First(b => b.TextContent.Contains("Save Changes"));
        okBtn.Click();
        okClicked.Should().BeTrue();
    }

    [Fact]
    public void Modal_NumericWidth_AppendsPxAndConstrainsWidth()
    {
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.Width, "500")
            .AddChildContent("Modal Body"));

        var dialogContainer = cut.Find(".relative.transform");
        dialogContainer.GetAttribute("style").Should().Contain("max-width: 500px");
        dialogContainer.GetAttribute("style").Should().Contain("width: 100%");
    }

    [Theory]
    [InlineData(AlertType.Success, "bg-emerald-50", "status")]
    [InlineData(AlertType.Info, "bg-sky-50", "status")]
    [InlineData(AlertType.Warning, "bg-amber-50", "alert")]
    [InlineData(AlertType.Error, "bg-rose-50", "alert")]
    public void Alert_SemanticVariants_ApplyCorrectColorsAndAriaRoles(AlertType type, string bgClass, string ariaRole)
    {
        var cut = RenderComponent<Alert>(parameters => parameters
            .Add(p => p.Type, type)
            .Add(p => p.Message, "Alert notification"));

        var el = cut.Find($"[role='{ariaRole}']");
        el.ClassList.Should().Contain(bgClass);
        el.TextContent.Should().Contain("Alert notification");
    }

    [Fact]
    public void Alert_ShowIcon_ControlsIconVisibilityEvenWithDescription()
    {
        var withoutIcon = RenderComponent<Alert>(parameters => parameters
            .Add(p => p.Message, "Title")
            .Add(p => p.Description, "Some description")
            .Add(p => p.ShowIcon, false));

        withoutIcon.FindAll("svg").Should().BeEmpty();

        var withIcon = RenderComponent<Alert>(parameters => parameters
            .Add(p => p.Message, "Title")
            .Add(p => p.Description, "Some description")
            .Add(p => p.ShowIcon, true));

        withIcon.FindAll("svg").Should().NotBeEmpty();
    }

    [Fact]
    public void Spin_RendersRoleStatusAndAriaLivePolite()
    {
        var cut = RenderComponent<Spin>(parameters => parameters
            .Add(p => p.Spinning, true)
            .Add(p => p.Tip, "Loading data...")
            .AddChildContent("<div>Protected Content</div>"));

        var status = cut.Find("[role='status']");
        status.GetAttribute("aria-live").Should().Be("polite");
        cut.Markup.Should().Contain("Loading data...");
    }

    [Fact]
    public void MessageService_DispatchesToastMessages()
    {
        var service = Services.GetRequiredService<IMessageService>();
        var cut = RenderComponent<MessageHost>();

        service.Success("Saved item successfully");

        cut.Render();
        cut.Markup.Should().Contain("Saved item successfully");
    }

    [Fact]
    public void Tooltip_RendersTriggerAndTooltipContent()
    {
        var cut = RenderComponent<Tooltip>(parameters => parameters
            .Add(p => p.Title, "Helpful hint")
            .Add(p => p.Visible, true)
            .AddChildContent("<button>Hover me</button>"));

        cut.Markup.Should().Contain("Hover me");
        cut.Markup.Should().Contain("Helpful hint");
    }
}
