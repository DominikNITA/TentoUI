using System;
using System.Collections.Concurrent;
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

public class AdversarialStressTests : TestContext
{
    public record StressItem(int Id, string? Name, decimal Value);

    public AdversarialStressTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTentoUI();
    }

    #region 1. DatePicker Adversarial Boundaries

    [Fact]
    public void DatePicker_MinValue_OpenAndClickFirstDay_SelectsCorrectDateWithoutCrashing()
    {
        DateTime? selectedDate = null;
        var cut = RenderComponent<DatePicker>(parameters => parameters
            .Add(p => p.Value, new DateTime(1, 1, 15))
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, v => selectedDate = v)));

        // Open popover
        cut.Find(".tento-datepicker > div").Click();

        // Check disabled state of prev buttons at MinValue (Year 1, Month 1)
        var prevMonthBtn = cut.Find("button[aria-label='Previous month']");
        var prevYearBtn = cut.Find("button[aria-label='Previous year']");
        prevMonthBtn.HasAttribute("disabled").Should().BeTrue();
        prevYearBtn.HasAttribute("disabled").Should().BeTrue();

        // Attempting to click disabled prev buttons must not throw
        Action actPrevMonth = () => prevMonthBtn.Click();
        actPrevMonth.Should().NotThrow();

        Action actPrevYear = () => prevYearBtn.Click();
        actPrevYear.Should().NotThrow();

        // The grid must render day cells
        var dayCells = cut.FindAll("div[role='grid'] button[role='gridcell']");
        dayCells.Should().NotBeEmpty();

        // First cell should be Day 1 of Month 1 (Year 1)
        dayCells[0].TextContent.Trim().Should().Be("1");

        // Click first cell
        Action actSelect = () => dayCells[0].Click();
        actSelect.Should().NotThrow();
        selectedDate.Should().Be(DateTime.MinValue.Date);
    }

    [Fact]
    public void DatePicker_MaxValue_OpenAndClickLastDay_SelectsCorrectDateWithoutCrashing()
    {
        DateTime? selectedDate = null;
        var cut = RenderComponent<DatePicker>(parameters => parameters
            .Add(p => p.Value, DateTime.MaxValue)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, v => selectedDate = v)));

        // Open popover
        cut.Find(".tento-datepicker > div").Click();

        // Check disabled state of next buttons at MaxValue (Year 9999, Month 12)
        var nextMonthBtn = cut.Find("button[aria-label='Next month']");
        var nextYearBtn = cut.Find("button[aria-label='Next year']");
        nextMonthBtn.HasAttribute("disabled").Should().BeTrue();
        nextYearBtn.HasAttribute("disabled").Should().BeTrue();

        // Attempting to click disabled next buttons must not throw
        Action actNextMonth = () => nextMonthBtn.Click();
        actNextMonth.Should().NotThrow();

        Action actNextYear = () => nextYearBtn.Click();
        actNextYear.Should().NotThrow();

        // The grid must render day cells
        var dayCells = cut.FindAll("div[role='grid'] button[role='gridcell']");
        dayCells.Should().NotBeEmpty();

        // Last cell should be 31 (December 31, 9999)
        var lastCell = dayCells.Last();
        lastCell.TextContent.Trim().Should().Be("31");

        // Click last cell
        Action actSelect = () => lastCell.Click();
        actSelect.Should().NotThrow();
        selectedDate.Should().Be(DateTime.MaxValue.Date);
    }

    [Fact]
    public void DatePicker_MonthNavigation_TransitionBoundaries()
    {
        // Start at Nov 9999, navigate to Dec 9999, then try navigating further
        var cut = RenderComponent<DatePicker>(parameters => parameters
            .Add(p => p.Value, new DateTime(9999, 11, 15)));

        cut.Find(".tento-datepicker > div").Click();

        var nextMonthBtn = cut.Find("button[aria-label='Next month']");
        nextMonthBtn.HasAttribute("disabled").Should().BeFalse();

        // Click to Dec 9999
        nextMonthBtn.Click();
        cut.Markup.Should().Contain("December 9999");

        // Now nextMonth button must be disabled
        nextMonthBtn = cut.Find("button[aria-label='Next month']");
        nextMonthBtn.HasAttribute("disabled").Should().BeTrue();
        Action actExceed = () => nextMonthBtn.Click();
        actExceed.Should().NotThrow();
    }

    [Fact]
    public void DatePicker_ManualInput_EdgeCasesAndMalformedStrings()
    {
        DateTime? boundDate = DateTime.Today;
        var cut = RenderComponent<DatePicker>(parameters => parameters
            .Add(p => p.Value, boundDate)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, v => boundDate = v)));

        var input = cut.Find("input");

        // 1. Valid extreme min
        input.Change("0001-01-01");
        boundDate.Should().Be(new DateTime(1, 1, 1));

        // 2. Valid extreme max
        input.Change("9999-12-31");
        boundDate.Should().Be(new DateTime(9999, 12, 31));

        // 3. Clear with whitespace / empty
        input.Change("");
        boundDate.Should().BeNull();

        // 4. Malformed date should not crash
        Action actMalformed = () => input.Change("not-a-valid-date");
        actMalformed.Should().NotThrow();

        // 5. Overflow year should not crash
        Action actOverflow = () => input.Change("10000-01-01");
        actOverflow.Should().NotThrow();
    }

    #endregion

    #region 2. ModalHost Decoupling & Custom IModalService

    private class NonConformingModalService : IModalService
    {
        public event Action? OnModalChanged;
        private readonly System.Collections.Generic.List<ModalInstance> _active = new();
        public IReadOnlyList<ModalInstance> ActiveModals => _active;
        public bool ResolveCalled { get; private set; }
        public bool LastResult { get; private set; }

        public Task<bool> ConfirmAsync(ModalConfirmOptions options)
        {
            var inst = new ModalInstance(options);
            _active.Add(inst);
            OnModalChanged?.Invoke();
            return inst.Tcs.Task;
        }

        public Task<bool> ConfirmAsync(string title, string content, ModalType type = ModalType.Confirm) =>
            ConfirmAsync(new ModalConfirmOptions { Title = title, Content = content, Type = type });

        public Task InfoAsync(string title, string content) => ConfirmAsync(title, content, ModalType.Info);
        public Task SuccessAsync(string title, string content) => ConfirmAsync(title, content, ModalType.Success);
        public Task WarningAsync(string title, string content) => ConfirmAsync(title, content, ModalType.Warning);
        public Task ErrorAsync(string title, string content) => ConfirmAsync(title, content, ModalType.Error);

        public void ResolveModal(Guid id, bool result)
        {
            ResolveCalled = true;
            LastResult = result;
            var inst = System.Linq.Enumerable.FirstOrDefault(_active, m => m.Id == id);
            if (inst != null)
            {
                _active.Remove(inst);
                inst.Tcs.TrySetResult(result);
                OnModalChanged?.Invoke();
            }
        }

        public void CloseAll()
        {
            _active.Clear();
            OnModalChanged?.Invoke();
        }
    }

    [Fact]
    public async Task ModalHost_CustomService_HandlesOkAndCancelWithoutTypeCastException()
    {
        var custom = new NonConformingModalService();
        var localCtx = new TestContext();
        localCtx.JSInterop.Mode = JSRuntimeMode.Loose;
        localCtx.Services.AddSingleton<IModalService>(custom);

        var cut = localCtx.RenderComponent<ModalHost>();

        // Trigger modal
        var task = custom.ConfirmAsync("Custom Title", "Custom Message");
        cut.Render();

        cut.Markup.Should().Contain("Custom Title");
        cut.Markup.Should().Contain("Custom Message");

        // Find OK button
        var okBtn = cut.Find("button.tento-btn-primary");
        Action actOk = () => okBtn.Click();
        actOk.Should().NotThrow("ModalHost must not cast to concrete ModalService on Ok click");
        custom.ResolveCalled.Should().BeTrue();
        custom.LastResult.Should().BeTrue();
        var okResult = await task;
        okResult.Should().BeTrue();

        // Trigger another modal and test Cancel
        var task2 = custom.ConfirmAsync("Modal 2", "Cancel Test");
        cut.Render();
        var cancelBtn = cut.Find("button.tento-btn-default");
        Action actCancel = () => cancelBtn.Click();
        actCancel.Should().NotThrow("ModalHost must not cast to concrete ModalService on Cancel click");
        custom.LastResult.Should().BeFalse();
        var cancelResult = await task2;
        cancelResult.Should().BeFalse();
    }

    [Fact]
    public void ModalHost_StackingMultipleModals_RendersAllAndClosesCleanly()
    {
        var service = new ModalService();
        var localCtx = new TestContext();
        localCtx.JSInterop.Mode = JSRuntimeMode.Loose;
        localCtx.Services.AddSingleton<IModalService>(service);

        var cut = localCtx.RenderComponent<ModalHost>();

        // Open 3 modals
        _ = service.InfoAsync("Modal A", "Content A");
        _ = service.WarningAsync("Modal B", "Content B");
        _ = service.ErrorAsync("Modal C", "Content C");

        cut.Render();
        service.ActiveModals.Should().HaveCount(3);
        cut.FindAll("[role='dialog']").Should().HaveCount(3);

        // Close all
        service.CloseAll();
        cut.Render();
        service.ActiveModals.Should().BeEmpty();
        cut.FindAll("[role='dialog']").Should().BeEmpty();
    }

    #endregion

    #region 3. MessageService Concurrency & High Stress

    [Fact]
    public async Task MessageService_HighContentionStress_NoDeadlocksOrExceptions()
    {
        var service = new MessageService();
        var capturedExceptions = new ConcurrentBag<Exception>();
        const int threadCount = 30;
        const int opsPerThread = 20;

        var tasks = Enumerable.Range(0, threadCount).Select(t => Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < opsPerThread; i++)
                {
                    var msg = service.Success($"Msg {t}-{i}", durationSeconds: 0.05);

                    // Read snapshot while mutating
                    var snapshot = service.Messages;
                    _ = snapshot.Count;

                    // Update message
                    msg.Update($"Updated {t}-{i}", MessageType.Warning, 0.05);

                    // Idempotent duplicate close
                    msg.Close();
                    msg.Close();
                }
            }
            catch (Exception ex)
            {
                capturedExceptions.Add(ex);
            }
        }));

        await Task.WhenAll(tasks);
        capturedExceptions.Should().BeEmpty("Concurrent access and mutation of MessageService must be completely thread-safe");
    }

    [Fact]
    public async Task MessageService_DestroyConcurrentlyWithAdds_DoesNotThrow()
    {
        var service = new MessageService();
        var capturedExceptions = new ConcurrentBag<Exception>();

        var addTasks = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
        {
            try
            {
                for (int j = 0; j < 30; j++)
                {
                    service.Info($"Info {i}-{j}", durationSeconds: 0.1);
                }
            }
            catch (Exception ex)
            {
                capturedExceptions.Add(ex);
            }
        }));

        var destroyTask = Task.Run(async () =>
        {
            try
            {
                for (int k = 0; k < 10; k++)
                {
                    await Task.Delay(5);
                    service.Destroy();
                }
            }
            catch (Exception ex)
            {
                capturedExceptions.Add(ex);
            }
        });

        await Task.WhenAll(addTasks.Concat(new[] { destroyTask }));
        capturedExceptions.Should().BeEmpty("Calling Destroy() concurrently with additions must not corrupt collection or throw");
    }

    [Fact]
    public void MessageItem_OnCloseCallback_ExecutesExactlyOnceUnderRaceConditions()
    {
        var service = new MessageService();
        int callbackCount = 0;

        var msg = service.Open(new MessageOptions
        {
            Content = "Callback test",
            DurationSeconds = 10,
            OnClose = () => Interlocked.Increment(ref callbackCount)
        });

        // Parallel rapid closes
        Parallel.For(0, 50, _ =>
        {
            msg.Close();
        });

        callbackCount.Should().Be(1, "OnClose must be invoked exactly once even under extreme concurrent Close() calls");
    }

    #endregion

    #region 4. Table Edge Cases: Zero/Negative PageSize & Null/Empty Data

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Table_NonPositivePageSize_SafelyClampsToMinimumOne(int pageSize)
    {
        var data = new System.Collections.Generic.List<StressItem> { new(1, "Item 1", 10m), new(2, "Item 2", 20m) };

        var cut = RenderComponent<Table<StressItem>>(parameters => parameters
            .Add(p => p.DataSource, (IEnumerable<StressItem>)data)
            .Add(p => p.PageSize, pageSize)
            .Add(p => p.ShowPagination, true)
            .Add(p => p.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<PropertyColumn<StressItem, int>>(0);
                builder.AddAttribute(1, "Title", "ID");
                builder.AddAttribute(2, "Property", (System.Linq.Expressions.Expression<Func<StressItem, int>>)(x => x.Id));
                builder.CloseComponent();
            })));

        // Must render without OutOfMemoryException or DivideByZeroException
        cut.Find(".tento-table-container").Should().NotBeNull();
        cut.Markup.Should().Contain("Showing 1 to 1 of 2");
    }

    [Fact]
    public void Table_EmptyDataSource_RendersAccurateZeroSummary()
    {
        var cut = RenderComponent<Table<StressItem>>(parameters => parameters
            .Add(p => p.DataSource, Enumerable.Empty<StressItem>())
            .Add(p => p.ShowPagination, true)
            .Add(p => p.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<PropertyColumn<StressItem, int>>(0);
                builder.AddAttribute(1, "Title", "ID");
                builder.AddAttribute(2, "Property", (System.Linq.Expressions.Expression<Func<StressItem, int>>)(x => x.Id));
                builder.CloseComponent();
            })));

        cut.Markup.Should().Contain("Showing 0 to 0 of 0", "Empty data table must show 0 to 0 of 0 instead of 1 to 0 of 0");
        cut.Markup.Should().Contain("No data available");
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(999999)]
    public void Table_ExtremePageIndex_ClampsCleanlyWithoutCrashing(int extremePageIndex)
    {
        var data = new System.Collections.Generic.List<StressItem> { new(1, "Item 1", 10m), new(2, "Item 2", 20m) };

        var cut = RenderComponent<Table<StressItem>>(parameters => parameters
            .Add(p => p.DataSource, (IEnumerable<StressItem>)data)
            .Add(p => p.PageSize, 1)
            .Add(p => p.PageIndex, extremePageIndex)
            .Add(p => p.ShowPagination, true)
            .Add(p => p.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<PropertyColumn<StressItem, int>>(0);
                builder.AddAttribute(1, "Title", "ID");
                builder.AddAttribute(2, "Property", (System.Linq.Expressions.Expression<Func<StressItem, int>>)(x => x.Id));
                builder.CloseComponent();
            })));

        cut.Find(".tento-table-container").Should().NotBeNull();
    }

    [Fact]
    public void Table_Sorting_AllNullValues_CyclesSortingWithoutCrashing()
    {
        var data = new System.Collections.Generic.List<StressItem>
        {
            new(1, null, 10m),
            new(2, null, 20m),
            new(3, null, 30m)
        };

        var cut = RenderComponent<Table<StressItem>>(parameters => parameters
            .Add(p => p.DataSource, (IEnumerable<StressItem>)data)
            .Add(p => p.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<PropertyColumn<StressItem, string?>>(0);
                builder.AddAttribute(1, "Title", "Name");
                builder.AddAttribute(2, "Property", (System.Linq.Expressions.Expression<Func<StressItem, string?>>)(x => x.Name));
                builder.AddAttribute(3, "Sortable", true);
                builder.CloseComponent();
            })));

        var header = cut.Find("th[aria-sort]");

        // Cycle through: None -> Asc -> Desc -> None
        Action cycleSort = () =>
        {
            header.Click(); // Asc
            header.Click(); // Desc
            header.Click(); // None
        };

        cycleSort.Should().NotThrow("Sorting column where all values are null must not throw");
    }

    #endregion

    #region 5. Select & Tabs Boundary Cases

    [Fact]
    public void Select_ExtremeNumberOfOptions_RendersAndFiltersWithoutCrashing()
    {
        var largeDataset = Enumerable.Range(1, 1000).Select(i => new StressItem(i, $"Item {i}", i * 1.5m)).ToList();

        var cut = RenderComponent<Select<int, StressItem>>(parameters => parameters
            .Add(p => p.DataSource, (IEnumerable<StressItem>)largeDataset)
            .Add(p => p.ValueProperty, x => x.Id)
            .Add(p => p.LabelProperty, x => x.Name ?? string.Empty));

        // Open
        cut.Find("[role='combobox']").Click();

        var options = cut.FindAll("[role='option']");
        options.Should().HaveCount(1000);
    }

    [Fact]
    public void Tabs_RapidKeyboardNavigation_OnEmptyTabs_DoesNotCrash()
    {
        var cut = RenderComponent<Tabs>();
        var nav = cut.Find(".tento-tabs-nav");

        Action act = () =>
        {
            for (int i = 0; i < 20; i++)
            {
                nav.KeyDown("ArrowRight");
                nav.KeyDown("ArrowLeft");
            }
        };

        act.Should().NotThrow();
    }

    #endregion

    #region 6. Additional Adversarial Stress Scenarios

    [Fact]
    public void DatePicker_NavigatingBackwardsRepeatedly_ClampsAtYear1Month1()
    {
        var cut = RenderComponent<DatePicker>(parameters => parameters
            .Add(p => p.Value, new DateTime(1, 3, 1)));

        cut.Find(".tento-datepicker > div").Click();

        var prevMonthBtn = cut.Find("button[aria-label='Previous month']");
        // Click 10 times backwards
        for (int i = 0; i < 10; i++)
        {
            prevMonthBtn.Click();
        }

        cut.Markup.Should().Contain("January 0001");
        prevMonthBtn.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void DatePicker_NavigatingForwardsRepeatedly_ClampsAtYear9999Month12()
    {
        var cut = RenderComponent<DatePicker>(parameters => parameters
            .Add(p => p.Value, new DateTime(9999, 10, 1)));

        cut.Find(".tento-datepicker > div").Click();

        var nextMonthBtn = cut.Find("button[aria-label='Next month']");
        // Click 10 times forwards
        for (int i = 0; i < 10; i++)
        {
            nextMonthBtn.Click();
        }

        cut.Markup.Should().Contain("December 9999");
        nextMonthBtn.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Modal_EscapeKey_WhenKeyboardIsFalse_DoesNotDismiss()
    {
        bool closed = false;
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.Keyboard, false)
            .Add(p => p.OnCancel, () => closed = true));

        var dialog = cut.Find("[role='dialog']");
        dialog.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        closed.Should().BeFalse("Escape key must be ignored when Keyboard is false");
    }

    [Fact]
    public void Modal_EscapeKey_WhenConfirmLoadingIsTrue_DoesNotDismiss()
    {
        bool closed = false;
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.ConfirmLoading, true)
            .Add(p => p.OnCancel, () => closed = true));

        var dialog = cut.Find("[role='dialog']");
        dialog.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        closed.Should().BeFalse("Escape key must be ignored when ConfirmLoading is true");
    }

    [Fact]
    public void Table_LargeDataSource_FastPaginationAndSorting()
    {
        var largeData = Enumerable.Range(1, 5000)
            .Select(i => new StressItem(i, $"Item {5000 - i}", i * 2.5m))
            .ToList();

        var cut = RenderComponent<Table<StressItem>>(parameters => parameters
            .Add(p => p.DataSource, (IEnumerable<StressItem>)largeData)
            .Add(p => p.PageSize, 25)
            .Add(p => p.ShowPagination, true)
            .Add(p => p.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<PropertyColumn<StressItem, int>>(0);
                builder.AddAttribute(1, "Title", "ID");
                builder.AddAttribute(2, "Property", (System.Linq.Expressions.Expression<Func<StressItem, int>>)(x => x.Id));
                builder.AddAttribute(3, "Sortable", true);
                builder.CloseComponent();

                builder.OpenComponent<PropertyColumn<StressItem, string?>>(10);
                builder.AddAttribute(11, "Title", "Name");
                builder.AddAttribute(12, "Property", (System.Linq.Expressions.Expression<Func<StressItem, string?>>)(x => x.Name));
                builder.AddAttribute(13, "Sortable", true);
                builder.CloseComponent();
            })));

        cut.Markup.Should().Contain("Showing 1 to 25 of 5000");

        // Sort by ID
        var idHeader = cut.Find("th[aria-sort]");
        idHeader.Click(); // Asc
        idHeader.Click(); // Desc

        // Navigate page
        var nextBtn = cut.Find("button[aria-label='Next page']");
        nextBtn.Click();

        cut.Markup.Should().Contain("Showing 26 to 50 of 5000");
    }

    [Fact]
    public void MessageService_LoadingMessage_DoesNotAutoDismiss_UntilClose()
    {
        var service = new MessageService();
        var msg = service.Loading("Processing payment...", durationSeconds: 0);

        service.Messages.Should().HaveCount(1);
        service.Messages[0].Type.Should().Be(MessageType.Loading);

        // Does not auto dismiss
        msg.Close();
        service.Messages.Should().BeEmpty();
    }

    #endregion
}
