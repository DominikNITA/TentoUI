using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using TentoUI;

namespace TentoUI.Tests.Accessibility;

public class AccessibilityChallengeTests : TestContext
{
    public AccessibilityChallengeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTentoUI();
    }

    private class SampleModel
    {
        [Required(ErrorMessage = "Username is mandatory")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Age is mandatory")]
        public string? Age { get; set; }
    }

    // =========================================================================
    // Challenge 1: Modal Focus Trap & Keyboard Escape
    // =========================================================================

    [Fact]
    public void Modal_EscapeKey_DismissesModal_WhenMaskClosableIsTrue()
    {
        bool closed = false;
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.MaskClosable, true)
            .Add(p => p.OnCancel, () => closed = true));

        var dialog = cut.Find("[role='dialog']");
        dialog.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        closed.Should().BeTrue("Escape key should dismiss modal when MaskClosable is true");
    }

    [Fact]
    public void Modal_EscapeKey_SuppressedWhenMaskClosableIsFalse_EmpiricalDefect()
    {
        bool closed = false;
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.Closable, true)
            .Add(p => p.MaskClosable, false)
            .Add(p => p.OnCancel, () => closed = true));

        var dialog = cut.Find("[role='dialog']");
        dialog.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // Escape dismissal is decoupled from MaskClosable (WCAG 2.1.2)
        closed.Should().BeTrue("Escape key should dismiss modal when Closable is true even if MaskClosable is false");
    }

    [Fact]
    public void Modal_FocusTrap_InitialFocusAndTabindexContract()
    {
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.Title, "Trap Dialog")
            .AddChildContent("<input id='first-input' /><button id='second-btn'>Click</button>"));

        var dialog = cut.Find("[role='dialog']");
        dialog.GetAttribute("tabindex").Should().Be("-1", "Modal container should have tabindex -1 for focus management");
        dialog.GetAttribute("aria-modal").Should().Be("true");
    }

    // =========================================================================
    // Challenge 2: Form Validation Cascading & A11y Label Associations
    // =========================================================================

    [Fact]
    public void Form_WithEditContext_CascadesValidationErrors_ToFormItemAndInput()
    {
        var model = new SampleModel();
        var editContext = new EditContext(model);
        var messageStore = new ValidationMessageStore(editContext);

        var cut = RenderComponent<Form>(parameters => parameters
            .Add(p => p.EditContext, editContext)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<FormItem>(0);
                builder.AddAttribute(1, nameof(FormItem.For), (System.Linq.Expressions.Expression<Func<string?>>)(() => model.Username));
                builder.AddAttribute(2, nameof(FormItem.Label), "Username");
                builder.AddAttribute(3, nameof(FormItem.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<Input>(10);
                    b.AddAttribute(11, "Value", model.Username);
                    b.AddAttribute(12, "ValueExpression", (System.Linq.Expressions.Expression<Func<string?>>)(() => model.Username));
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            }));

        // Trigger validation failure
        var fieldId = new FieldIdentifier(model, nameof(SampleModel.Username));
        cut.InvokeAsync(() =>
        {
            messageStore.Add(fieldId, "Username is mandatory");
            editContext.NotifyValidationStateChanged();
        });

        // 1. Verify FormItem renders alert message
        var alert = cut.Find("[role='alert']");
        alert.TextContent.Should().Contain("Username is mandatory");

        // 2. Verify Input has aria-invalid="true"
        var input = cut.Find("input");
        input.GetAttribute("aria-invalid").Should().Be("true", "Input must have aria-invalid='true' when invalid in EditContext");

        // 3. Verify Label 'for' matches Input 'id'
        var label = cut.Find("label");
        var labelFor = label.GetAttribute("for");
        var inputId = input.GetAttribute("id");
        labelFor.Should().NotBeNullOrEmpty();
        inputId.Should().Be(labelFor, "FormItem label 'for' must associate with Input 'id'");

        // 4. Verify Input aria-describedby matches the alert id
        var alertId = alert.GetAttribute("id");
        input.GetAttribute("aria-describedby").Should().Be(alertId);
    }

    [Fact]
    public void Form_WithModel_FailsToCascadeValidationToFormItem_EmpiricalDefect()
    {
        var model = new SampleModel(); // Username is null (required)

        var cut = RenderComponent<Form>(parameters => parameters
            .Add(p => p.Model, model)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();

                builder.OpenComponent<FormItem>(1);
                builder.AddAttribute(2, nameof(FormItem.For), (System.Linq.Expressions.Expression<Func<string?>>)(() => model.Username));
                builder.AddAttribute(3, nameof(FormItem.Label), "Username");
                builder.AddAttribute(4, nameof(FormItem.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<Input>(10);
                    b.AddAttribute(11, "Value", model.Username);
                    b.AddAttribute(12, "ValueExpression", (System.Linq.Expressions.Expression<Func<string?>>)(() => model.Username));
                    b.CloseComponent();
                }));
                builder.CloseComponent();

                builder.OpenComponent<Button>(20);
                builder.AddAttribute(21, "HtmlType", ButtonHtmlType.Submit);
                builder.AddAttribute(22, "ChildContent", (RenderFragment)(b => b.AddContent(23, "Submit")));
                builder.CloseComponent();
            }));

        // Submit the form
        var form = cut.Find("form");
        form.Submit();

        // 1. Verify FormItem renders alert message
        var alert = cut.FindAll("[role='alert']");
        alert.Should().NotBeEmpty("FormItem must display validation error when Form Model is invalid upon submit");
        alert[0].TextContent.Should().Contain("Username is mandatory");

        // 2. Verify Input has aria-invalid="true"
        var input = cut.Find("input");
        input.GetAttribute("aria-invalid").Should().Be("true", "Input must have aria-invalid='true' when invalid upon submit");

        // 3. Verify Label 'for' matches Input 'id'
        var label = cut.Find("label");
        var labelFor = label.GetAttribute("for");
        var inputId = input.GetAttribute("id");
        labelFor.Should().NotBeNullOrEmpty();
        inputId.Should().Be(labelFor, "FormItem label 'for' must associate with Input 'id'");

        // 4. Verify Input aria-describedby matches alert id
        input.GetAttribute("aria-describedby").Should().Be(alert[0].GetAttribute("id"));
    }

    [Fact]
    public void Select_InsideFormItem_LacksLabelIdAndAriaInvalid_EmpiricalDefect()
    {
        var cut = RenderComponent<FormItem>(parameters => parameters
            .Add(p => p.FieldId, "country-select")
            .Add(p => p.Label, "Country")
            .Add(p => p.Error, "Please select a country")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<Select<string, string>>(0);
                builder.AddAttribute(1, "DataSource", new[] { "USA", "Canada" });
                builder.CloseComponent();
            }));

        var label = cut.Find("label");
        label.GetAttribute("for").Should().Be("country-select");

        // 1. Verify Select renders id="@ElementId" matching label 'for'
        var elementWithId = cut.FindAll("#country-select");
        elementWithId.Should().NotBeEmpty("Select must render element with id matching FormItem FieldId");

        // 2. Verify combobox trigger has role="combobox" and id="country-select"
        var combobox = cut.Find("[role='combobox']");
        combobox.GetAttribute("id").Should().Be("country-select");

        // 3. Verify combobox trigger emits aria-invalid="true" when error exists
        combobox.GetAttribute("aria-invalid").Should().Be("true");

        // 4. Verify combobox trigger emits aria-describedby="country-select-error"
        combobox.GetAttribute("aria-describedby").Should().Be("country-select-error");
    }

    // =========================================================================
    // Challenge 3: Tabs Roving Tabindex & Keyboard Navigation
    // =========================================================================

    [Fact]
    public void Tabs_RovingTabindex_ActiveTabHasZero_InactiveTabsHaveMinusOne()
    {
        var cut = RenderComponent<Tabs>(parameters => parameters
            .Add(p => p.DefaultActiveKey, "tab1")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<TabPane>(0);
                builder.AddAttribute(1, "Key", "tab1");
                builder.AddAttribute(2, "Tab", "Tab One");
                builder.CloseComponent();

                builder.OpenComponent<TabPane>(10);
                builder.AddAttribute(11, "Key", "tab2");
                builder.AddAttribute(12, "Tab", "Tab Two");
                builder.CloseComponent();

                builder.OpenComponent<TabPane>(20);
                builder.AddAttribute(21, "Key", "tab3");
                builder.AddAttribute(22, "Tab", "Tab Three");
                builder.CloseComponent();
            }));

        var tabs = cut.FindAll("button[role='tab']");
        tabs.Should().HaveCount(3);

        // Tab 1 active
        tabs[0].GetAttribute("tabindex").Should().Be("0");
        tabs[0].GetAttribute("aria-selected").Should().Be("true");

        // Tab 2 & 3 inactive
        tabs[1].GetAttribute("tabindex").Should().Be("-1");
        tabs[1].GetAttribute("aria-selected").Should().Be("false");
        tabs[2].GetAttribute("tabindex").Should().Be("-1");
        tabs[2].GetAttribute("aria-selected").Should().Be("false");
    }

    [Fact]
    public void Tabs_KeyboardNavigation_ArrowRight_CyclesToNextTab()
    {
        var cut = RenderComponent<Tabs>(parameters => parameters
            .Add(p => p.DefaultActiveKey, "tab1")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<TabPane>(0);
                builder.AddAttribute(1, "Key", "tab1");
                builder.AddAttribute(2, "Tab", "Tab One");
                builder.CloseComponent();

                builder.OpenComponent<TabPane>(10);
                builder.AddAttribute(11, "Key", "tab2");
                builder.AddAttribute(12, "Tab", "Tab Two");
                builder.CloseComponent();
            }));

        var tablist = cut.Find("[role='tablist']");
        tablist.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        var tabs = cut.FindAll("button[role='tab']");
        // Tab 2 should now be active with tabindex 0
        tabs[0].GetAttribute("tabindex").Should().Be("-1");
        tabs[0].GetAttribute("aria-selected").Should().Be("false");
        tabs[1].GetAttribute("tabindex").Should().Be("0");
        tabs[1].GetAttribute("aria-selected").Should().Be("true");
    }

    // =========================================================================
    // Challenge 4: Menu Roving Tabindex & Keyboard Navigation
    // =========================================================================

    [Fact]
    public void Menu_LacksTabindexAndKeyboardNavigation_EmpiricalDefect()
    {
        var cut = RenderComponent<Menu>(parameters => parameters
            .Add(p => p.SelectedKey, "m1")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<MenuItem>(0);
                builder.AddAttribute(1, "Key", "m1");
                builder.AddAttribute(2, "Title", "Dashboard");
                builder.CloseComponent();

                builder.OpenComponent<MenuItem>(10);
                builder.AddAttribute(11, "Key", "m2");
                builder.AddAttribute(12, "Title", "Settings");
                builder.CloseComponent();
            }));

        var items = cut.FindAll("[role='menuitem']");
        items.Should().HaveCount(2);

        // Selected item has tabindex 0, unselected has tabindex -1
        var tabIndexAttr0 = items[0].GetAttribute("tabindex");
        var tabIndexAttr1 = items[1].GetAttribute("tabindex");

        tabIndexAttr0.Should().Be("0", "Selected MenuItem must have tabindex='0' for roving tabindex");
        tabIndexAttr1.Should().Be("-1", "Unselected MenuItem must have tabindex='-1'");

        // Keyboard navigation via ArrowDown cycles selection to next item
        items[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        items = cut.FindAll("[role='menuitem']");
        items[0].GetAttribute("tabindex").Should().Be("-1");
        items[1].GetAttribute("tabindex").Should().Be("0");
    }
}
