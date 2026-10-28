using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TentoUI.Tests.Accessibility;

public class InteractionAccessibilityStressTests : TestContext
{
    public InteractionAccessibilityStressTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTentoUI();
    }

    private class ComplexFormModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Full Name must be between 3 and 50 characters.")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Country selection is required.")]
        public string? Country { get; set; }
    }

    // =========================================================================
    // 1. Menu Roving Tabindex & Advanced Keyboard Interactivity
    // =========================================================================

    [Fact]
    public void Menu_RovingTabindex_InitialState_CorrectTabindices()
    {
        var cut = RenderComponent<Menu>(parameters => parameters
            .Add(p => p.SelectedKey, "item2")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<MenuItem>(0);
                builder.AddAttribute(1, "Key", "item1");
                builder.AddAttribute(2, "Title", "Item 1");
                builder.CloseComponent();

                builder.OpenComponent<MenuItem>(10);
                builder.AddAttribute(11, "Key", "item2");
                builder.AddAttribute(12, "Title", "Item 2");
                builder.CloseComponent();

                builder.OpenComponent<MenuItem>(20);
                builder.AddAttribute(21, "Key", "item3");
                builder.AddAttribute(22, "Title", "Item 3");
                builder.CloseComponent();
            }));

        var items = cut.FindAll("[role='menuitem']");
        items.Should().HaveCount(3);

        items[0].GetAttribute("tabindex").Should().Be("-1");
        items[0].GetAttribute("aria-selected").Should().Be("false");

        items[1].GetAttribute("tabindex").Should().Be("0");
        items[1].GetAttribute("aria-selected").Should().Be("true");

        items[2].GetAttribute("tabindex").Should().Be("-1");
        items[2].GetAttribute("aria-selected").Should().Be("false");
    }

    [Fact]
    public void Menu_KeyboardNavigation_SkipsDisabledItems_AndWrapsCorrectly()
    {
        string? currentSelected = "item1";

        var cut = RenderComponent<Menu>(parameters => parameters
            .Add(p => p.SelectedKey, currentSelected)
            .Add(p => p.SelectedKeyChanged, EventCallback.Factory.Create<string>(this, k => currentSelected = k))
            .AddChildContent(builder =>
            {
                builder.OpenComponent<MenuItem>(0);
                builder.AddAttribute(1, "Key", "item1");
                builder.AddAttribute(2, "Title", "First Enabled");
                builder.CloseComponent();

                builder.OpenComponent<MenuItem>(10);
                builder.AddAttribute(11, "Key", "item2");
                builder.AddAttribute(12, "Disabled", true);
                builder.AddAttribute(13, "Title", "Second Disabled");
                builder.CloseComponent();

                builder.OpenComponent<MenuItem>(20);
                builder.AddAttribute(21, "Key", "item3");
                builder.AddAttribute(22, "Title", "Third Enabled");
                builder.CloseComponent();
            }));

        var items = cut.FindAll("[role='menuitem']");
        items[0].GetAttribute("tabindex").Should().Be("0");
        items[1].GetAttribute("tabindex").Should().Be("-1");
        items[1].GetAttribute("aria-disabled").Should().Be("true");
        items[2].GetAttribute("tabindex").Should().Be("-1");

        // ArrowDown on item1 should skip disabled item2 and select item3
        items[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Render();

        items = cut.FindAll("[role='menuitem']");
        items[0].GetAttribute("tabindex").Should().Be("-1");
        items[1].GetAttribute("tabindex").Should().Be("-1");
        items[2].GetAttribute("tabindex").Should().Be("0");

        // ArrowDown on item3 should wrap back to item1 (skipping item2)
        items[2].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Render();

        items = cut.FindAll("[role='menuitem']");
        items[0].GetAttribute("tabindex").Should().Be("0");
        items[2].GetAttribute("tabindex").Should().Be("-1");

        // ArrowUp on item1 should wrap backward to item3 (skipping item2)
        items[0].KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        cut.Render();

        items = cut.FindAll("[role='menuitem']");
        items[0].GetAttribute("tabindex").Should().Be("-1");
        items[2].GetAttribute("tabindex").Should().Be("0");
    }

    [Fact]
    public void Menu_HorizontalMode_RespondsToArrowLeftAndArrowRight()
    {
        var cut = RenderComponent<Menu>(parameters => parameters
            .Add(p => p.Mode, MenuMode.Horizontal)
            .Add(p => p.SelectedKey, "h1")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<MenuItem>(0);
                builder.AddAttribute(1, "Key", "h1");
                builder.AddAttribute(2, "Title", "Tab 1");
                builder.CloseComponent();

                builder.OpenComponent<MenuItem>(10);
                builder.AddAttribute(11, "Key", "h2");
                builder.AddAttribute(12, "Title", "Tab 2");
                builder.CloseComponent();
            }));

        var items = cut.FindAll("[role='menuitem']");
        items[0].GetAttribute("tabindex").Should().Be("0");

        // ArrowRight moves to h2
        items[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        cut.Render();

        items = cut.FindAll("[role='menuitem']");
        items[0].GetAttribute("tabindex").Should().Be("-1");
        items[1].GetAttribute("tabindex").Should().Be("0");

        // ArrowLeft moves back to h1
        items[1].KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        cut.Render();

        items = cut.FindAll("[role='menuitem']");
        items[0].GetAttribute("tabindex").Should().Be("0");
        items[1].GetAttribute("tabindex").Should().Be("-1");
    }

    [Fact]
    public void Menu_EnterAndSpaceKey_TriggersSelectionAndClick()
    {
        bool clicked = false;
        var cut = RenderComponent<Menu>(parameters => parameters
            .Add(p => p.SelectedKey, "m1")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<MenuItem>(0);
                builder.AddAttribute(1, "Key", "m1");
                builder.AddAttribute(2, "Title", "Item 1");
                builder.AddAttribute(3, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicked = true));
                builder.CloseComponent();
            }));

        var item = cut.Find("[role='menuitem']");
        item.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        clicked.Should().BeTrue("Pressing Enter on MenuItem should invoke click callback");

        clicked = false;
        item.KeyDown(new KeyboardEventArgs { Key = " " });
        clicked.Should().BeTrue("Pressing Space on MenuItem should invoke click callback");
    }

    // =========================================================================
    // 2. Form Model Validation Cascading & Multi-Field Error Propagation
    // =========================================================================

    [Fact]
    public void Form_WithComplexModel_PropagatesErrorsToMultipleFormItemsAndInputs()
    {
        var model = new ComplexFormModel(); // All fields null/invalid initially

        var cut = RenderComponent<Form>(parameters => parameters
            .Add(p => p.Model, model)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();

                // Field 1: FullName
                builder.OpenComponent<FormItem>(1);
                builder.AddAttribute(2, nameof(FormItem.For), (System.Linq.Expressions.Expression<Func<string?>>)(() => model.FullName));
                builder.AddAttribute(3, nameof(FormItem.Label), "Full Name");
                builder.AddAttribute(4, nameof(FormItem.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<Input>(10);
                    b.AddAttribute(11, "Value", model.FullName);
                    b.AddAttribute(12, "ValueExpression", (System.Linq.Expressions.Expression<Func<string?>>)(() => model.FullName));
                    b.CloseComponent();
                }));
                builder.CloseComponent();

                // Field 2: Email
                builder.OpenComponent<FormItem>(20);
                builder.AddAttribute(21, nameof(FormItem.For), (System.Linq.Expressions.Expression<Func<string?>>)(() => model.Email));
                builder.AddAttribute(22, nameof(FormItem.Label), "Email Address");
                builder.AddAttribute(23, nameof(FormItem.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<Input>(30);
                    b.AddAttribute(31, "Value", model.Email);
                    b.AddAttribute(32, "ValueExpression", (System.Linq.Expressions.Expression<Func<string?>>)(() => model.Email));
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            }));

        // Submit form
        var form = cut.Find("form");
        form.Submit();
        cut.Render();

        // 1. Verify alerts for both fields
        var alerts = cut.FindAll("[role='alert']");
        alerts.Should().HaveCount(2, "Both invalid fields must render role='alert'");
        alerts[0].TextContent.Should().Contain("Full Name is required.");
        alerts[1].TextContent.Should().Contain("Email Address is required.");

        // 2. Verify inputs have aria-invalid="true" and correct aria-describedby
        var inputs = cut.FindAll("input");
        inputs.Should().HaveCount(2);

        inputs[0].GetAttribute("aria-invalid").Should().Be("true");
        inputs[0].GetAttribute("aria-describedby").Should().Be(alerts[0].GetAttribute("id"));

        inputs[1].GetAttribute("aria-invalid").Should().Be("true");
        inputs[1].GetAttribute("aria-describedby").Should().Be(alerts[1].GetAttribute("id"));

        // 3. Verify label 'for' attributes point to input 'id's
        var labels = cut.FindAll("label");
        labels.Should().HaveCount(2);
        labels[0].GetAttribute("for").Should().Be(inputs[0].GetAttribute("id"));
        labels[1].GetAttribute("for").Should().Be(inputs[1].GetAttribute("id"));
    }

    [Fact]
    public void FormItem_ExplicitErrorString_RendersAlertAndSetsAriaInvalid()
    {
        var cut = RenderComponent<FormItem>(parameters => parameters
            .Add(p => p.FieldId, "custom-id")
            .Add(p => p.Label, "Custom Field")
            .Add(p => p.Error, "Server-side rejection error")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<Input>(0);
                builder.CloseComponent();
            }));

        var alert = cut.Find("[role='alert']");
        alert.TextContent.Should().Contain("Server-side rejection error");
        alert.GetAttribute("id").Should().Be("custom-id-error");

        var input = cut.Find("input");
        input.GetAttribute("id").Should().Be("custom-id");
        input.GetAttribute("aria-invalid").Should().Be("true");
        input.GetAttribute("aria-describedby").Should().Be("custom-id-error");
    }

    // =========================================================================
    // 3. Select Component A11y & Form Integration Contracts
    // =========================================================================

    [Fact]
    public void Select_WithinFormItem_TriggerHasMatchingIdAndAriaAttributes()
    {
        var cut = RenderComponent<FormItem>(parameters => parameters
            .Add(p => p.FieldId, "state-select")
            .Add(p => p.Label, "State")
            .Add(p => p.Error, "State is required.")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<Select<string, string>>(0);
                builder.AddAttribute(1, "DataSource", new[] { "California", "New York", "Texas" });
                builder.CloseComponent();
            }));

        // Label points to state-select
        var label = cut.Find("label");
        label.GetAttribute("for").Should().Be("state-select");

        // Combobox trigger must have id="state-select"
        var combobox = cut.Find("[role='combobox']");
        combobox.GetAttribute("id").Should().Be("state-select");
        combobox.GetAttribute("aria-invalid").Should().Be("true");
        combobox.GetAttribute("aria-describedby").Should().Be("state-select-error");
        combobox.GetAttribute("aria-controls").Should().Be("state-select-listbox");
    }

    [Fact]
    public void Select_KeyboardInteraction_NavigatesOptionsAndSelects()
    {
        string? selectedValue = null;
        var items = new[] { "Apple", "Banana", "Cherry" };

        var cut = RenderComponent<Select<string, string>>(parameters => parameters
            .Add(p => p.DataSource, items)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, v => selectedValue = v)));

        var trigger = cut.Find("[role='combobox']");
        trigger.GetAttribute("aria-expanded").Should().Be("false");

        // 1. ArrowDown opens dropdown (leaves highlight at initial state)
        trigger.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Render();
        trigger = cut.Find("[role='combobox']");
        trigger.GetAttribute("aria-expanded").Should().Be("true");

        // 2. ArrowDown highlights first option (Apple, index 0)
        trigger.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Render();

        // 3. ArrowDown highlights second option (Banana, index 1)
        trigger.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Render();

        // 4. Enter selects highlighted option (Banana)
        trigger.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        cut.Render();

        selectedValue.Should().Be("Banana");
        trigger = cut.Find("[role='combobox']");
        trigger.GetAttribute("aria-expanded").Should().Be("false");
    }

    [Fact]
    public void Select_EscapeKey_ClosesDropdownWithoutSelecting()
    {
        var items = new[] { "Option A", "Option B" };
        var cut = RenderComponent<Select<string, string>>(parameters => parameters
            .Add(p => p.DataSource, items));

        var trigger = cut.Find("[role='combobox']");
        trigger.Click(); // Open dropdown
        trigger.GetAttribute("aria-expanded").Should().Be("true");

        // Escape closes
        trigger.KeyDown(new KeyboardEventArgs { Key = "Escape" });
        cut.Render();
        trigger = cut.Find("[role='combobox']");
        trigger.GetAttribute("aria-expanded").Should().Be("false");
    }

    // =========================================================================
    // 4. Modal Escape Dismissal Decoupling & Focus Trapping Contract
    // =========================================================================

    [Fact]
    public void Modal_EscapeKey_Dismisses_WhenMaskClosableIsFalse()
    {
        bool cancelInvoked = false;
        bool visibleValue = true;

        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.MaskClosable, false)
            .Add(p => p.Keyboard, true)
            .Add(p => p.Closable, true)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true))
            .Add(p => p.VisibleChanged, EventCallback.Factory.Create<bool>(this, v => visibleValue = v)));

        var dialog = cut.Find("[role='dialog']");
        dialog.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cancelInvoked.Should().BeTrue("Escape key must dismiss modal when Keyboard is true, regardless of MaskClosable");
        visibleValue.Should().BeFalse();
    }

    [Fact]
    public void Modal_EscapeKey_Ignored_WhenKeyboardIsFalse()
    {
        bool cancelInvoked = false;

        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.Keyboard, false)
            .Add(p => p.Closable, true)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true)));

        var dialog = cut.Find("[role='dialog']");
        dialog.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cancelInvoked.Should().BeFalse("Escape key must be ignored when Keyboard is false");
    }

    [Fact]
    public void Modal_EscapeKey_Ignored_WhenConfirmLoadingIsTrue()
    {
        bool cancelInvoked = false;

        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.Keyboard, true)
            .Add(p => p.Closable, true)
            .Add(p => p.ConfirmLoading, true)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true)));

        var dialog = cut.Find("[role='dialog']");
        dialog.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cancelInvoked.Should().BeFalse("Escape key must not dismiss modal while ConfirmLoading is true (async operation underway)");
    }

    [Fact]
    public void Modal_EscapeKey_Ignored_WhenClosableIsFalse()
    {
        bool cancelInvoked = false;

        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.Keyboard, true)
            .Add(p => p.Closable, false)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true)));

        var dialog = cut.Find("[role='dialog']");
        dialog.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cancelInvoked.Should().BeFalse("Escape key must not dismiss modal when Closable is false");
    }

    [Fact]
    public void Modal_AriaContract_DialogAttributesAndHeaderIds()
    {
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.Title, "A11y Verification Dialog")
            .AddChildContent("<p>Accessible content paragraph</p>"));

        var dialog = cut.Find("[role='dialog']");
        dialog.GetAttribute("aria-modal").Should().Be("true");
        dialog.GetAttribute("tabindex").Should().Be("-1");

        var labelledBy = dialog.GetAttribute("aria-labelledby");
        var describedBy = dialog.GetAttribute("aria-describedby");

        labelledBy.Should().NotBeNullOrEmpty();
        describedBy.Should().NotBeNullOrEmpty();

        var titleEl = cut.Find($"#{labelledBy}");
        titleEl.TextContent.Should().Contain("A11y Verification Dialog");

        var bodyEl = cut.Find($"#{describedBy}");
        bodyEl.TextContent.Should().Contain("Accessible content paragraph");
    }
}
