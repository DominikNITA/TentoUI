using Microsoft.Extensions.DependencyInjection;
using TentoUI.Docs.Components;
using TentoUI.Docs.Services;

namespace TentoUI.Tests.Components;

public class V11ComponentsAndDocsTests : TestContext
{
    public V11ComponentsAndDocsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTentoUI();
        Services.AddSingleton<VersionService>();
    }

    [Fact]
    public void CopyButton_ClicksAndShowsCopiedFeedback()
    {
        string? copiedValue = null;
        var cut = RenderComponent<CopyButton>(p => p
            .Add(x => x.Text, "dotnet add package TentoUI")
            .Add(x => x.CopyLabel, "Copy Command")
            .Add(x => x.CopiedLabel, "Copied!")
            .Add(x => x.ResetDelayMs, 0)
            .Add(x => x.OnCopy, EventCallback.Factory.Create<string>(this, v => copiedValue = v)));

        var btn = cut.Find("button.tento-copy-button");
        btn.TextContent.Should().Contain("Copy Command");

        btn.Click();
        copiedValue.Should().Be("dotnet add package TentoUI");
        cut.Find("button.tento-copy-button").TextContent.Should().Contain("Copied!");
    }

    [Fact]
    public void Typography_Text_SupportsCopyableEditableKeyboardAndItalic()
    {
        string? edited = "Initial text";
        string? copied = null;

        var cut = RenderComponent<Text>(p => p
            .Add(x => x.Copyable, true)
            .Add(x => x.CopyText, "secret-token")
            .Add(x => x.Editable, true)
            .Add(x => x.EditText, edited)
            .Add(x => x.Keyboard, true)
            .Add(x => x.Italic, true)
            .Add(x => x.OnCopy, EventCallback.Factory.Create<string>(this, v => copied = v))
            .Add(x => x.EditTextChanged, EventCallback.Factory.Create<string>(this, v => edited = v))
            .AddChildContent("Ctrl+K"));

        cut.Find("kbd").Should().NotBeNull();
        cut.Find("i").Should().NotBeNull();

        // Click copy button
        cut.Find("button.tento-typography-copy-btn").Click();
        copied.Should().Be("secret-token");

        // Click edit button, type new text, and save
        cut.Find("button.tento-typography-edit-btn").Click();
        var input = cut.Find("input[aria-label='Edit text']");
        input.Input("Updated text");
        cut.Find("button[aria-label='Save edit']").Click();

        edited.Should().Be("Updated text");
    }

    [Fact]
    public void Flex_RendersDirectionJustifyAlignAndGap()
    {
        var cut = RenderComponent<Flex>(p => p
            .Add(x => x.Vertical, false)
            .Add(x => x.Justify, FlexJustify.SpaceBetween)
            .Add(x => x.Align, FlexAlign.Center)
            .Add(x => x.Gap, FlexGap.Middle)
            .Add(x => x.Wrap, FlexWrap.Wrap)
            .AddChildContent("<div>Item A</div><div>Item B</div>"));

        var root = cut.Find(".tento-flex");
        root.ClassList.Should().Contain("flex-row");
        root.ClassList.Should().Contain("justify-between");
        root.ClassList.Should().Contain("items-center");
        root.ClassList.Should().Contain("gap-4");
        root.ClassList.Should().Contain("flex-wrap");
    }

    [Fact]
    public void Splitter_SupportsKeyboardResizeAndCollapseToggle()
    {
        double split = 50;
        var cut = RenderComponent<Splitter>(p => p
            .Add(x => x.Split, split)
            .Add(x => x.Step, 10)
            .Add(x => x.Collapsible, true)
            .Add(x => x.SplitChanged, EventCallback.Factory.Create<double>(this, v => split = v))
            .AddChildContent(b =>
            {
                b.OpenComponent<SplitterPanel>(0);
                b.AddAttribute(1, "ChildContent", (RenderFragment)(cb => cb.AddContent(0, "Left Pane")));
                b.CloseComponent();

                b.OpenComponent<SplitterPanel>(2);
                b.AddAttribute(3, "ChildContent", (RenderFragment)(cb => cb.AddContent(0, "Right Pane")));
                b.CloseComponent();
            }));

        cut.Markup.Should().Contain("Left Pane");
        cut.Markup.Should().Contain("Right Pane");

        var separator = cut.Find("[role='separator']");
        separator.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        split.Should().Be(60);

        // Collapse toggle
        cut.Find(".tento-splitter-collapse-btn").Click();
        split.Should().Be(0);
    }

    [Fact]
    public void CommandPalette_FiltersNavigatesAndSelectsItem()
    {
        CommandItem? selected = null;
        bool open = true;
        var items = new[]
        {
            new CommandItem("cmd-1", "Create Project", "Workspace"),
            new CommandItem("cmd-2", "Toggle Theme", "Appearance")
        };

        var cut = RenderComponent<CommandPalette>(p => p
            .Add(x => x.Open, open)
            .Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => open = v))
            .Add(x => x.Items, items)
            .Add(x => x.OnSelect, EventCallback.Factory.Create<CommandItem>(this, item => selected = item)));

        var input = cut.Find("input[role='combobox']");
        input.Input("Theme");

        var options = cut.FindAll("button[role='option']");
        options.Should().HaveCount(1);
        options[0].TextContent.Should().Contain("Toggle Theme");

        options[0].Click();
        selected.Should().NotBeNull();
        selected!.Key.Should().Be("cmd-2");
        open.Should().BeFalse();
    }

    [Fact]
    public void ContextMenu_OpensOnRightClickAndInvokesAction()
    {
        string? clickedKey = null;
        var items = new[]
        {
            new DropdownMenuItem("rename", "Rename"),
            new DropdownMenuItem("delete", "Delete", Danger: true)
        };

        var cut = RenderComponent<ContextMenu>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.OnItemClick, EventCallback.Factory.Create<string>(this, k => clickedKey = k))
            .AddChildContent("<div class='target'>Right click here</div>"));

        cut.FindAll("[role='menu']").Should().BeEmpty();

        // Trigger contextmenu
        cut.Find(".tento-context-menu-trigger").ContextMenu();
        cut.Find("[role='menu']").Should().NotBeNull();

        cut.Find("button[data-menu-key='delete']").Click();
        clickedKey.Should().Be("delete");
        cut.FindAll("[role='menu']").Should().BeEmpty();
    }

    [Fact]
    public void Mentions_TriggersPopupOnAtSymbolAndInsertsSelection()
    {
        string? value = string.Empty;
        MentionOption? selectedOption = null;

        var cut = RenderComponent<Mentions>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.OptionsString, "dominik, sarah, alex")
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v))
            .Add(x => x.OnSelect, EventCallback.Factory.Create<MentionOption>(this, o => selectedOption = o)));

        var input = cut.Find("input[role='combobox']");
        input.Input("Hello @sa");

        var options = cut.FindAll("li[role='option']");
        options.Should().HaveCount(1);
        options[0].TextContent.Should().Contain("sarah");

        options[0].Click();
        value.Should().Be("Hello @sarah ");
        selectedOption.Should().NotBeNull();
        selectedOption!.Value.Should().Be("sarah");
    }

    [Fact]
    public void OTPInput_HandlesPasteAndInvokesOnComplete()
    {
        string? completedCode = null;
        string? boundValue = null;

        var cut = RenderComponent<OTPInput>(p => p
            .Add(x => x.Length, 6)
            .Add(x => x.ShowSeparator, true)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => boundValue = v))
            .Add(x => x.OnComplete, EventCallback.Factory.Create<string>(this, c => completedCode = c)));

        var slots = cut.FindAll("input.tento-otp-slot");
        slots.Should().HaveCount(6);
        cut.Find(".tento-otp-separator").Should().NotBeNull();

        // Paste full 6-digit code into first slot
        slots[0].Input("948201");
        boundValue.Should().Be("948201");
        completedCode.Should().Be("948201");
    }

    [Fact]
    public void DateRangePicker_AppliesPresetsAndSelectsRange()
    {
        DateRange? selectedRange = null;

        var cut = RenderComponent<DateRangePicker>(p => p
            .Add(x => x.ShowPresets, true)
            .Add(x => x.OnRangeChange, EventCallback.Factory.Create<DateRange>(this, r => selectedRange = r)));

        // Open popup
        cut.Find("[role='group']").Click();
        cut.Find(".tento-daterangepicker-popover").Should().NotBeNull();

        // Click "Last 7 Days" preset
        cut.Find("button:contains('Last 7 Days')").Click();
        selectedRange.Should().NotBeNull();
        selectedRange!.Start.Should().Be(DateTime.Today.AddDays(-6));
        selectedRange!.End.Should().Be(DateTime.Today);
    }

    [Fact]
    public void TimeRangePicker_SelectsBusinessHoursShortcut()
    {
        TimeRange? range = null;

        var cut = RenderComponent<TimeRangePicker>(p => p
            .Add(x => x.OnRangeChange, EventCallback.Factory.Create<TimeRange>(this, r => range = r)));

        cut.Find("[role='group']").Click();
        cut.Find(".tento-timerangepicker-popup").Should().NotBeNull();

        cut.Find("button:contains('Business Hours')").Click();
        range.Should().NotBeNull();
        range!.Start.Should().Be(new TimeOnly(9, 0));
        range!.End.Should().Be(new TimeOnly(17, 0));
    }

    [Fact]
    public void FilterBuilder_AddsRulesAndEmitsTableFilters()
    {
        IReadOnlyDictionary<string, string>? applied = null;

        var cut = RenderComponent<FilterBuilder>(p => p
            .Add(x => x.OnApply, EventCallback.Factory.Create<IReadOnlyDictionary<string, string>>(this, f => applied = f)));

        cut.FindAll(".tento-filter-rule-row").Should().HaveCount(1);

        // Add condition
        cut.Find("button:contains('Add Condition')").Click();
        cut.FindAll(".tento-filter-rule-row").Should().HaveCount(2);

        // Apply filters
        cut.Find("button:contains('Apply Filters')").Click();
        applied.Should().NotBeNull();
        applied!.Should().ContainKey("Status");
    }

    [Fact]
    public void ImagePreviewGroup_OpensGalleryAndNavigatesBetweenImages()
    {
        var cut = RenderComponent<ImagePreviewGroup>(p => p
            .AddChildContent(b =>
            {
                b.OpenComponent<Image>(0);
                b.AddAttribute(1, "Src", "https://example.com/img1.jpg");
                b.CloseComponent();

                b.OpenComponent<Image>(2);
                b.AddAttribute(3, "Src", "https://example.com/img2.jpg");
                b.CloseComponent();
            }));

        // Click first image preview overlay
        cut.FindAll(".group > div")[0].Click();
        cut.Find(".tento-image-preview-group-modal").Should().NotBeNull();
        cut.Find(".tento-image-preview-counter").TextContent.Trim().Should().Be("1 / 2");

        // Click next button
        cut.Find(".tento-image-preview-next").Click();
        cut.Find(".tento-image-preview-counter").TextContent.Trim().Should().Be("2 / 2");
    }

    [Fact]
    public void Sparkline_RendersSvgLineAreaAndBarModes()
    {
        var lineCut = RenderComponent<Sparkline>(p => p
            .Add(x => x.Type, SparklineType.Area)
            .Add(x => x.Values, "10, 20, 15, 35")
            .Add(x => x.ShowDots, true));

        lineCut.Find("svg[role='img']").Should().NotBeNull();
        lineCut.Find("polyline").Should().NotBeNull();
        lineCut.Find("polygon").Should().NotBeNull();
        lineCut.FindAll("circle").Should().HaveCount(4);

        var barCut = RenderComponent<Sparkline>(p => p
            .Add(x => x.Type, SparklineType.Bar)
            .Add(x => x.Values, "5, 10, 15"));
        barCut.FindAll("rect").Should().HaveCount(3);
    }

    [Fact]
    public void KanbanBoard_RendersColumnsAndMovesCardsBetweenColumns()
    {
        KanbanCardMoveArgs<KanbanCardItem>? moveArgs = null;
        var cards = new List<KanbanCardItem>
        {
            new() { Id = "1", Title = "Ship v1.1", ColumnKey = "todo", Tag = "Release" }
        };

        var cut = RenderComponent<KanbanBoard<KanbanCardItem>>(p => p
            .Add(x => x.Items, cards)
            .Add(x => x.OnCardMove, EventCallback.Factory.Create<KanbanCardMoveArgs<KanbanCardItem>>(this, a => moveArgs = a)));

        cut.FindAll(".tento-kanban-column").Should().HaveCount(3);
        cut.Markup.Should().Contain("Ship v1.1");

        // Move card from "To Do" to "In Progress"
        cut.Find("button[aria-label='Move to In Progress']").Click();
        moveArgs.Should().NotBeNull();
        moveArgs!.SourceColumnKey.Should().Be("todo");
        moveArgs!.TargetColumnKey.Should().Be("in-progress");
        cards[0].ColumnKey.Should().Be("in-progress");
    }

    [Fact]
    public void VersionService_PreservesRoutesAcrossVersionsAndDetectsUri()
    {
        var service = new VersionService();
        var nextVersion = service.AvailableVersions.First(v => v.IsPreview);
        var legacyVersion = service.AvailableVersions.First(v => v.IsLegacy);

        var route = VersionService.GetVersionRoute(nextVersion, "components/table");
        route.Should().Be("/next/components/table");

        var routeFromPrefixed = VersionService.GetVersionRoute(legacyVersion, "v1.0/components/button");
        routeFromPrefixed.Should().Be("/v0.9/components/button");

        service.DetectVersionFromUri("https://tentoui.dev/next/components/sparkline");
        service.CurrentVersion.IsPreview.Should().BeTrue();

        service.SwitchToLatest();
        service.CurrentVersion.IsLatest.Should().BeTrue();
    }

    [Fact]
    public void ComponentApiRegistry_ReflectsParametersAndSinceBadges()
    {
        var props = ComponentApiRegistry.GetProperties(typeof(Sparkline));
        props.Should().NotBeEmpty();
        props.Should().Contain(p => p.Property == "Values" && p.Since == "1.1.0");
        props.Should().Contain(p => p.Property == "Type" && p.Default == "SparklineType.Line");

        var cut = RenderComponent<ApiTable>(p => p
            .Add(x => x.Title, "Sparkline API")
            .Add(x => x.ComponentType, typeof(Sparkline)));

        cut.Markup.Should().Contain("Added in v1.1.0");
        cut.Markup.Should().Contain("Source-verified: Sparkline");
    }

    [Fact]
    public void TentoMarkupRenderer_RendersAllNewV11Components()
    {
        const string markup = """
            <div>
                <CopyButton Text="hello" />
                <Flex Gap="FlexGap.Small"><Tag>One</Tag><Tag>Two</Tag></Flex>
                <Sparkline Values="10, 20, 30" />
                <OTPInput Value="1234" Length="4" />
                <Mentions Value="@dominik" />
                <DateRangePicker />
                <TimeRangePicker />
                <FilterBuilder />
                <KanbanBoard />
            </div>
            """;

        var cut = RenderComponent<TentoMarkupRenderer>(p => p.Add(x => x.Markup, markup));
        cut.Find(".tento-copy-button").Should().NotBeNull();
        cut.Find(".tento-flex").Should().NotBeNull();
        cut.Find(".tento-sparkline").Should().NotBeNull();
        cut.Find(".tento-otp-input").Should().NotBeNull();
        cut.Find(".tento-mentions").Should().NotBeNull();
        cut.Find(".tento-daterangepicker").Should().NotBeNull();
        cut.Find(".tento-timerangepicker").Should().NotBeNull();
        cut.Find(".tento-filter-builder").Should().NotBeNull();
        cut.Find(".tento-kanban-board").Should().NotBeNull();
    }

    [Fact]
    public void CodeBlock_RendersSyntaxTokensLineNumbersAndCollapsibleState()
    {
        const string code = "public class Demo\n{\n    // comment\n    return 42;\n}";
        var cut = RenderComponent<CodeBlock>(p => p
            .Add(x => x.Title, "Demo.cs")
            .Add(x => x.Language, "csharp")
            .Add(x => x.Code, code)
            .Add(x => x.HighlightLines, "2,4")
            .Add(x => x.Collapsible, true));

        cut.Markup.Should().Contain("Demo.cs");
        cut.FindAll("tr").Should().HaveCount(5);

        // Collapse and expand
        var collapseBtn = cut.Find("button:contains('Collapse')");
        collapseBtn.Click();
        cut.FindAll("tr").Should().BeEmpty();

        cut.Find("button:contains('Expand')").Click();
        cut.FindAll("tr").Should().HaveCount(5);
    }

    [Fact]
    public void DiffViewer_ComputesDiffStatsAndSwitchesBetweenSplitAndUnified()
    {
        var cut = RenderComponent<DiffViewer>(p => p
            .Add(x => x.OldText, "line1\nline2")
            .Add(x => x.NewText, "line1\nline2_mod\nline3")
            .Add(x => x.SplitView, true));

        cut.Find("[aria-label='Added lines']").TextContent.Should().Be("+2");
        cut.Find("[aria-label='Removed lines']").TextContent.Should().Be("-1");
        cut.Find("table[aria-label='Side-by-side diff']").Should().NotBeNull();

        cut.Find("button:contains('Unified')").Click();
        cut.Find("table[aria-label='Unified diff']").Should().NotBeNull();
    }

    [Fact]
    public void MarkdownEditor_InsertsToolbarFormattingAndRendersPreview()
    {
        string? val = "# Title";
        var cut = RenderComponent<MarkdownEditor>(p => p
            .Add(x => x.Value, val)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => val = v)));

        cut.Find("button[aria-label='Bold']").Click();
        val.Should().Contain("**bold text**");

        cut.Find("button:contains('Preview')").Click();
        cut.Find("[aria-label='Markdown preview'] h1").TextContent.Should().Contain("Title");
    }

    [Fact]
    public void Gauge_And_MeterGroup_RenderAccessibleMeters()
    {
        var gaugeCut = RenderComponent<Gauge>(p => p
            .Add(x => x.Value, 88)
            .Add(x => x.Label, "CPU Usage"));

        var meterEl = gaugeCut.Find("[role='meter']");
        meterEl.GetAttribute("aria-valuenow").Should().Be("88");
        gaugeCut.Markup.Should().Contain("CPU Usage");

        var groupCut = RenderComponent<MeterGroup>(p => p
            .Add(x => x.Label, "Storage")
            .Add(x => x.TotalCapacity, 100)
            .Add(x => x.SegmentsString, "DB:40,Logs:20"));

        groupCut.Find("[role='meter']").GetAttribute("aria-valuenow").Should().Be("60");
        groupCut.Markup.Should().Contain("DB");
        groupCut.Markup.Should().Contain("Logs");
    }

    [Fact]
    public void ChatBubble_And_PromptInput_SupportReasoningFeedbackAndSuggestions()
    {
        ChatFeedback receivedFeedback = ChatFeedback.None;
        var bubbleCut = RenderComponent<ChatBubble>(p => p
            .Add(x => x.Author, "Copilot")
            .Add(x => x.Message, "Ready to assist.")
            .Add(x => x.ShowActions, true)
            .Add(x => x.ReasoningSteps, new[] { "Step 1", "Step 2" })
            .Add(x => x.OnFeedback, EventCallback.Factory.Create<ChatFeedback>(this, f => receivedFeedback = f)));

        // Expand reasoning steps
        bubbleCut.Find("button:contains('Thought process')").Click();
        bubbleCut.Markup.Should().Contain("Step 1");

        // Click helpful button
        bubbleCut.Find("button[aria-label='Helpful']").Click();
        receivedFeedback.Should().Be(ChatFeedback.Like);

        string? submittedPrompt = null;
        var promptCut = RenderComponent<PromptInput>(p => p
            .Add(x => x.SuggestionsString, "Explain CQRS,Generate test")
            .Add(x => x.OnSubmit, EventCallback.Factory.Create<string>(this, s => submittedPrompt = s)));

        promptCut.Find("button:contains('Explain CQRS')").Click();
        submittedPrompt.Should().Be("Explain CQRS");
    }

    [Fact]
    public void DataGridVirtual_RendersStickyAndResizableColumns()
    {
        var data = Enumerable.Range(1, 50).Select(i => new KanbanCardItem { Id = i.ToString(), Title = $"Row {i}" }).ToList();

        var cut = RenderComponent<DataGridVirtual<KanbanCardItem>>(p => p
            .Add(x => x.DataSource, data)
            .Add(x => x.ChildContent, b =>
            {
                b.OpenComponent<TableColumn<KanbanCardItem>>(0);
                b.AddAttribute(1, "Title", "ID");
                b.AddAttribute(2, "Field", "Id");
                b.AddAttribute(3, "Fixed", ColumnFixed.Left);
                b.CloseComponent();

                b.OpenComponent<TableColumn<KanbanCardItem>>(4);
                b.AddAttribute(5, "Title", "Title");
                b.AddAttribute(6, "Field", "Title");
                b.AddAttribute(7, "Resizable", true);
                b.CloseComponent();
            }));

        cut.Find("th.sticky.left-0").Should().NotBeNull();
        cut.Find(".tento-table-resize-handle").Should().NotBeNull();
    }

    [Fact]
    public void CronBuilder_GeneratesValidFiveFieldCronExpressions()
    {
        string? cron = null;
        var cut = RenderComponent<CronBuilder>(p => p
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => cron = v)));

        // Default is Daily 09:00 -> "0 9 * * *"
        cron.Should().Be("0 9 * * *");

        // Switch to Minutes mode
        cut.Find("button[role='tab']:contains('Minutes')").Click();
        cron.Should().Be("*/5 * * * *");
    }

    [Fact]
    public void SignaturePad_RecordsPointerStrokesAndExportsSvgDataUri()
    {
        string? dataUri = null;
        var cut = RenderComponent<SignaturePad>(p => p
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => dataUri = v)));

        var svg = cut.Find("svg[role='img']");
        svg.PointerDown(new PointerEventArgs { OffsetX = 10, OffsetY = 20 });
        svg.PointerMove(new PointerEventArgs { OffsetX = 50, OffsetY = 60 });
        svg.PointerUp(new PointerEventArgs { OffsetX = 50, OffsetY = 60 });

        dataUri.Should().NotBeNull();
        dataUri.Should().StartWith("data:image/svg+xml;utf8,");
        cut.FindAll("path").Should().HaveCount(1);

        cut.Find("button:contains('Clear')").Click();
        dataUri.Should().BeNull();
    }

    [Fact]
    public void AvatarUpload_SupportsZoomRotateAndRemove()
    {
        string? src = "https://example.com/avatar.png";
        var cut = RenderComponent<AvatarUpload>(p => p
            .Add(x => x.Src, src)
            .Add(x => x.ShowCropControls, true)
            .Add(x => x.SrcChanged, EventCallback.Factory.Create<string?>(this, v => src = v)));

        cut.Find("img").GetAttribute("style").Should().Contain("rotate(0deg)");

        cut.Find("button:contains('Rotate 90°')").Click();
        cut.Find("img").GetAttribute("style").Should().Contain("rotate(90deg)");

        cut.Find("button:contains('Remove')").Click();
        src.Should().BeNull();
    }

    [Fact]
    public void Kbd_And_HotkeyListener_RenderKeycapsAndTriggerShortcut()
    {
        var kbdCut = RenderComponent<Kbd>(p => p.Add(x => x.Keys, "Ctrl+Shift+P"));
        kbdCut.FindAll("kbd").Should().HaveCount(3);

        string? triggered = null;
        var hotkeyCut = RenderComponent<HotkeyListener>(p => p
            .Add(x => x.Shortcut, "Ctrl+S")
            .Add(x => x.OnTrigger, EventCallback.Factory.Create<string>(this, s => triggered = s)));

        hotkeyCut.Find("[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "s", CtrlKey = true });
        triggered.Should().Be("Ctrl+S");
    }
}
