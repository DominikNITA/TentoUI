using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TentoUI.Tests.Stress;

public class EmpiricalStressTests : TestContext
{
    public record TestModel(int Id, string? Name);

    public record OptionItem(int Id, string Name);

    #region 1. DatePicker Extreme Dates

    [Fact]
    public void DatePicker_ExtremeDate_MaxValue_ShouldNotThrowOnOpen()
    {
        // DatePicker with DateTime.MaxValue (9999-12-31)
        var cut = RenderComponent<DatePicker>(parameters => parameters
            .Add(p => p.Value, DateTime.MaxValue));

        // Click trigger wrapper to open popover
        var trigger = cut.Find(".tento-datepicker > div");
        Action act = () => trigger.Click();

        act.Should().NotThrow<ArgumentOutOfRangeException>("DatePicker popover must not crash when rendering month calendar for DateTime.MaxValue");
    }

    [Fact]
    public void DatePicker_ExtremeDate_MinValue_ShouldNotThrowOnOpen()
    {
        // DatePicker with DateTime.MinValue (0001-01-01)
        var cut = RenderComponent<DatePicker>(parameters => parameters
            .Add(p => p.Value, DateTime.MinValue));

        var trigger = cut.Find(".tento-datepicker > div");
        Action act = () => trigger.Click();

        act.Should().NotThrow<ArgumentOutOfRangeException>("DatePicker popover must not crash when rendering month calendar for DateTime.MinValue");
    }

    [Fact]
    public void DatePicker_ExtremeDate_NextMonthOnYear9999_ShouldNotThrow()
    {
        var cut = RenderComponent<DatePicker>(parameters => parameters
            .Add(p => p.Value, new DateTime(9999, 12, 1)));

        var trigger = cut.Find(".tento-datepicker > div");
        trigger.Click();

        var nextMonthBtn = cut.Find("button[aria-label='Next month']");
        Action act = () => nextMonthBtn.Click();

        act.Should().NotThrow<ArgumentOutOfRangeException>("Navigating to next month on Year 9999 must not throw unhandled ArgumentOutOfRangeException");
    }

    [Fact]
    public void DatePicker_ExtremeDate_PrevMonthOnYear1_ShouldNotThrow()
    {
        var cut = RenderComponent<DatePicker>(parameters => parameters
            .Add(p => p.Value, new DateTime(1, 1, 1)));

        var trigger = cut.Find(".tento-datepicker > div");
        trigger.Click();

        var prevMonthBtn = cut.Find("button[aria-label='Previous month']");
        Action act = () => prevMonthBtn.Click();

        act.Should().NotThrow<ArgumentOutOfRangeException>("Navigating to previous month on Year 1 must not throw unhandled ArgumentOutOfRangeException");
    }

    #endregion

    #region 2. Table Null / Empty Collections & Edge Cases

    [Fact]
    public void Table_NullDataSource_RendersEmptyTableGracefully()
    {
        var cut = RenderComponent<Table<TestModel>>(parameters => parameters
            .Add(p => p.DataSource, (IEnumerable<TestModel>?)null)
            .Add(p => p.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<PropertyColumn<TestModel, int>>(0);
                builder.AddAttribute(1, "Title", "ID");
                builder.AddAttribute(2, "Property", (System.Linq.Expressions.Expression<Func<TestModel, int>>)(x => x.Id));
                builder.CloseComponent();
            })));

        cut.Markup.Should().Contain("No data available");
    }

    [Fact]
    public void Table_EmptyDataSource_WithPagination_RendersProperPageCounts()
    {
        var cut = RenderComponent<Table<TestModel>>(parameters => parameters
            .Add(p => p.DataSource, Enumerable.Empty<TestModel>())
            .Add(p => p.ShowPagination, true)
            .Add(p => p.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<PropertyColumn<TestModel, int>>(0);
                builder.AddAttribute(1, "Title", "ID");
                builder.AddAttribute(2, "Property", (System.Linq.Expressions.Expression<Func<TestModel, int>>)(x => x.Id));
                builder.CloseComponent();
            })));

        // Total items is 0
        cut.Markup.Should().Contain("of 0");
        var footer = cut.Find(".tento-table-container");
        footer.Should().NotBeNull();
    }

    [Fact]
    public void Table_ZeroPageSize_DoesNotThrow()
    {
        var data = new System.Collections.Generic.List<TestModel> { new(1, "Alice"), new(2, "Bob") };
        var cut = RenderComponent<Table<TestModel>>(parameters => parameters
            .Add(p => p.DataSource, (IEnumerable<TestModel>)data)
            .Add(p => p.PageSize, 0)
            .Add(p => p.ShowPagination, true)
            .Add(p => p.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<PropertyColumn<TestModel, int>>(0);
                builder.AddAttribute(1, "Title", "ID");
                builder.AddAttribute(2, "Property", (System.Linq.Expressions.Expression<Func<TestModel, int>>)(x => x.Id));
                builder.CloseComponent();
            })));

        cut.Should().NotBeNull();
    }

    [Fact]
    public void Table_Sorting_WithNullValues_DoesNotThrow()
    {
        var data = new System.Collections.Generic.List<TestModel>
        {
            new(1, null),
            new(2, "Bob"),
            new(3, "Alice")
        };

        var cut = RenderComponent<Table<TestModel>>(parameters => parameters
            .Add(p => p.DataSource, (IEnumerable<TestModel>)data)
            .Add(p => p.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<PropertyColumn<TestModel, string?>>(0);
                builder.AddAttribute(1, "Title", "Name");
                builder.AddAttribute(2, "Property", (System.Linq.Expressions.Expression<Func<TestModel, string?>>)(x => x.Name));
                builder.AddAttribute(3, "Sortable", true);
                builder.CloseComponent();
            })));

        var header = cut.Find("th[aria-sort]");
        Action act = () => header.Click();
        act.Should().NotThrow("Sorting columns containing null values must not throw");
    }

    #endregion

    #region 3. Select Null / Empty Collections & Edge Cases

    [Fact]
    public void Select_NullDataSource_OpensDropdownGracefully()
    {
        var cut = RenderComponent<Select<int, OptionItem>>(parameters => parameters
            .Add(p => p.DataSource, (IEnumerable<OptionItem>?)null));

        var trigger = cut.Find("[role='combobox']");
        Action act = () => trigger.Click();
        act.Should().NotThrow();

        cut.Markup.Should().Contain("No options found");
    }

    [Fact]
    public void Select_EmptyDataSource_KeyboardNavigation_DoesNotThrow()
    {
        var cut = RenderComponent<Select<int, OptionItem>>(parameters => parameters
            .Add(p => p.DataSource, Enumerable.Empty<OptionItem>()));

        var selectDiv = cut.Find(".tento-select");
        Action act = () =>
        {
            selectDiv.KeyDown("ArrowDown");
            selectDiv.KeyDown("ArrowDown");
            selectDiv.KeyDown("ArrowUp");
            selectDiv.KeyDown("Enter");
            selectDiv.KeyDown("Escape");
        };

        act.Should().NotThrow("Keyboard navigation on empty Select must not throw index out of bounds");
    }

    [Fact]
    public void Select_MultipleMode_NullValues_AllowsSelectingItems()
    {
        var items = new System.Collections.Generic.List<OptionItem> { new(1, "First"), new(2, "Second") };
        IEnumerable<int>? selectedValues = null;

        var cut = RenderComponent<Select<int, OptionItem>>(parameters => parameters
            .Add(p => p.Mode, SelectMode.Multiple)
            .Add(p => p.DataSource, (IEnumerable<OptionItem>)items)
            .Add(p => p.ValueProperty, x => x.Id)
            .Add(p => p.LabelProperty, x => x.Name)
            .Add(p => p.Values, selectedValues)
            .Add(p => p.ValuesChanged, EventCallback.Factory.Create<IEnumerable<int>>(this, v => selectedValues = v)));

        // Open
        cut.Find("[role='combobox']").Click();

        var options = cut.FindAll("[role='option']");
        options.Count.Should().Be(2);

        // Click first option
        Action act = () => options[0].Click();
        act.Should().NotThrow("Multiple Select with null initial Values parameter must not throw NullReferenceException");

        selectedValues.Should().NotBeNull();
        selectedValues.Should().Contain(1);
    }

    #endregion

    #region 4. Typography & Grid Col Boundary Values

    [Theory]
    [InlineData(0, "h1")]
    [InlineData(-10, "h1")]
    [InlineData(7, "h6")]
    [InlineData(100, "h6")]
    public void Typography_Title_ClampsExtremeLevels(int level, string expectedTag)
    {
        var cut = RenderComponent<Title>(parameters => parameters
            .Add(p => p.Level, level)
            .AddChildContent("Boundary Test"));

        cut.Find(expectedTag).Should().NotBeNull();
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    [InlineData(24)]
    [InlineData(50)]
    public void Grid_Col_ExtremeSpans_RendersWithoutCrashing(int span)
    {
        var cut = RenderComponent<Col>(parameters => parameters
            .Add(p => p.Span, span)
            .AddChildContent("Col Content"));

        cut.Markup.Should().Contain("tento-col");
    }

    #endregion

    #region 5. Button Rapid Clicks & Loading State

    [Fact]
    public void Button_RapidClicks_WhenLoading_SuppressesAllClicks()
    {
        int clickCount = 0;
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Loading, true)
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clickCount++))
            .AddChildContent("Submit"));

        var button = cut.Find("button");
        for (int i = 0; i < 50; i++)
        {
            button.Click();
        }

        clickCount.Should().Be(0, "Rapid clicks while button is Loading must not invoke OnClick");
    }

    [Fact]
    public void Button_RapidClicks_WhenDisabled_SuppressesAllClicks()
    {
        int clickCount = 0;
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clickCount++))
            .AddChildContent("Submit"));

        var button = cut.Find("button");
        for (int i = 0; i < 50; i++)
        {
            button.Click();
        }

        clickCount.Should().Be(0, "Rapid clicks while button is Disabled must not invoke OnClick");
    }

    #endregion

    #region 6. Concurrency & Rapid State Mutation

    [Fact]
    public async Task MessageService_RapidConcurrentAddAndClose_ThreadSafety()
    {
        var service = new MessageService();
        var capturedExceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
        {
            try
            {
                var msg = service.Success($"Message {i}", durationSeconds: 0.05);
                msg.Update($"Updated Message {i}");
                msg.Close();
            }
            catch (Exception ex)
            {
                capturedExceptions.Add(ex);
            }
        }));

        await Task.WhenAll(tasks);
        capturedExceptions.Count.Should().Be(0, "MessageService must be safe against concurrent message creation, updates, and close operations");
    }

    [Fact]
    public void Tabs_HandleKeyDown_WithEmptyPanes_DoesNotThrow()
    {
        var cut = RenderComponent<Tabs>();
        var nav = cut.Find(".tento-tabs-nav");

        Action act = () =>
        {
            nav.KeyDown("ArrowRight");
            nav.KeyDown("ArrowLeft");
            nav.KeyDown("ArrowDown");
            nav.KeyDown("ArrowUp");
        };

        act.Should().NotThrow("Keyboard navigation on Tabs with no panes must not throw");
    }

    private class CustomModalService : IModalService
    {
        public event Action? OnModalChanged;
        private readonly System.Collections.Generic.List<ModalInstance> _modals = new();
        public System.Collections.Generic.IReadOnlyList<ModalInstance> ActiveModals => _modals;

        public Task<bool> ConfirmAsync(ModalConfirmOptions options)
        {
            var inst = new ModalInstance(options);
            _modals.Add(inst);
            OnModalChanged?.Invoke();
            return inst.Tcs.Task;
        }

        public Task<bool> ConfirmAsync(string title, string content, ModalType type = ModalType.Confirm) =>
            ConfirmAsync(new ModalConfirmOptions { Title = title, Content = content, Type = type });

        public Task InfoAsync(string title, string content) => ConfirmAsync(title, content, ModalType.Info);
        public Task SuccessAsync(string title, string content) => ConfirmAsync(title, content, ModalType.Success);
        public Task WarningAsync(string title, string content) => ConfirmAsync(title, content, ModalType.Warning);
        public Task ErrorAsync(string title, string content) => ConfirmAsync(title, content, ModalType.Error);
        public void CloseAll() { _modals.Clear(); OnModalChanged?.Invoke(); }
    }

    [Fact]
    public void ModalHost_WithCustomIModalService_ShouldNotThrowInvalidCastException()
    {
        var customService = new CustomModalService();
        Services.AddSingleton<IModalService>(customService);

        var cut = RenderComponent<ModalHost>();

        // Trigger a modal
        _ = customService.ConfirmAsync("Test", "Content");

        // Click OK on modal
        var okBtn = cut.Find("button.tento-btn-primary");
        Action act = () => okBtn.Click();

        act.Should().NotThrow<InvalidCastException>("ModalHost must not tightly couple and hardcast IModalService to concrete ModalService");
    }

    #endregion
}
