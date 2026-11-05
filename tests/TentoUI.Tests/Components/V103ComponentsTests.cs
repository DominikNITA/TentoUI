using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace TentoUI.Tests.Components;

public class V103ComponentsTests : TestContext
{
    public V103ComponentsTests()
    {
        Services.AddTentoUI();
    }

    [Fact]
    public void KanbanBoard_RendersColumnsAndMovesCard()
    {
        var columns = new[]
        {
            new KanbanColumnDefinition("todo", "To Do"),
            new KanbanColumnDefinition("done", "Done")
        };

        var cards = new List<KanbanCardItem>
        {
            new() { Id = "k1", Title = "Card Alpha", ColumnKey = "todo" }
        };

        KanbanCardMoveArgs<KanbanCardItem>? moveResult = null;

        var cut = RenderComponent<KanbanBoard<KanbanCardItem>>(p => p
            .Add(x => x.Columns, columns)
            .Add(x => x.Items, cards)
            .Add(x => x.OnCardMove, EventCallback.Factory.Create<KanbanCardMoveArgs<KanbanCardItem>>(this, args => moveResult = args)));

        cut.Find(".tento-kanban-board").Should().NotBeNull();
        cut.Markup.Should().Contain("Card Alpha");

        // Move right via accessible button
        var moveBtn = cut.Find("button[aria-label='Move to Done']");
        moveBtn.Click();

        moveResult.Should().NotBeNull();
        moveResult!.SourceColumnKey.Should().Be("todo");
        moveResult.TargetColumnKey.Should().Be("done");
        cards[0].ColumnKey.Should().Be("done");
    }

    [Fact]
    public void KanbanBoard_QuickAddButton_InvokesCallback()
    {
        string? addedCol = null;
        var columns = new[] { new KanbanColumnDefinition("todo", "To Do") };

        var cut = RenderComponent<KanbanBoard<KanbanCardItem>>(p => p
            .Add(x => x.Columns, columns)
            .Add(x => x.ShowAddButton, true)
            .Add(x => x.OnAddCard, EventCallback.Factory.Create<string>(this, col => addedCol = col)));

        var addBtn = cut.Find("button:contains('Add Task')");
        addBtn.Click();

        addedCol.Should().Be("todo");
    }

    [Fact]
    public void FormWizard_AdvancesStepWhenValidationPasses()
    {
        int currentStep = 0;
        var cut = RenderComponent<FormWizard>(p => p
            .Add(x => x.CurrentStep, currentStep)
            .Add(x => x.CurrentStepChanged, EventCallback.Factory.Create<int>(this, s => currentStep = s))
            .AddChildContent(b =>
            {
                b.OpenComponent<WizardStep>(0);
                b.AddAttribute(1, "Title", "Step One");
                b.AddAttribute(2, "ChildContent", (RenderFragment)(c => c.AddContent(0, "Content for Step 1")));
                b.CloseComponent();

                b.OpenComponent<WizardStep>(3);
                b.AddAttribute(4, "Title", "Step Two");
                b.AddAttribute(5, "ChildContent", (RenderFragment)(c => c.AddContent(0, "Content for Step 2")));
                b.CloseComponent();
            }));

        cut.Find(".tento-form-wizard").Should().NotBeNull();
        cut.Markup.Should().Contain("Content for Step 1");

        // Click Next
        cut.Find("button:contains('Next')").Click();
        currentStep.Should().Be(1);
    }

    [Fact]
    public void FormWizard_CancelsStepTransitionWhenBeforeStepChangeReturnsFalse()
    {
        int currentStep = 0;
        var cut = RenderComponent<FormWizard>(p => p
            .Add(x => x.CurrentStep, currentStep)
            .Add(x => x.BeforeStepChange, new Func<WizardStepChangeArgs, Task<bool>>(_ => Task.FromResult(false)))
            .AddChildContent(b =>
            {
                b.OpenComponent<WizardStep>(0);
                b.AddAttribute(1, "Title", "Step One");
                b.CloseComponent();

                b.OpenComponent<WizardStep>(2);
                b.AddAttribute(3, "Title", "Step Two");
                b.CloseComponent();
            }));

        cut.Find("button:contains('Next')").Click();
        currentStep.Should().Be(0); // Navigation aborted
    }

    [Fact]
    public void FormWizard_InvokesOnFinishOnLastStep()
    {
        bool finished = false;
        var cut = RenderComponent<FormWizard>(p => p
            .Add(x => x.CurrentStep, 1)
            .Add(x => x.OnFinish, EventCallback.Factory.Create(this, () => finished = true))
            .AddChildContent(b =>
            {
                b.OpenComponent<WizardStep>(0);
                b.AddAttribute(1, "Title", "Step One");
                b.CloseComponent();

                b.OpenComponent<WizardStep>(2);
                b.AddAttribute(3, "Title", "Step Two");
                b.CloseComponent();
            }));

        cut.Find("button:contains('Finish')").Click();
        finished.Should().BeTrue();
    }

    public class TestStepperModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    [Fact]
    public void StepperForm_RendersHorizontalAndVerticalLayouts()
    {
        var model = new TestStepperModel { Name = "Alice", Email = "alice@example.com" };

        var cut = RenderComponent<StepperForm<TestStepperModel>>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Layout, StepperFormLayout.Vertical)
            .AddChildContent(b =>
            {
                b.OpenComponent<StepperFormStep>(0);
                b.AddAttribute(1, "Title", "Personal Info");
                b.AddAttribute(2, "ChildContent", (RenderFragment)(c => c.AddContent(0, "Step 1 fields")));
                b.CloseComponent();
            }));

        cut.Find(".tento-stepper-form").Should().NotBeNull();
        cut.Markup.Should().Contain("Step 1 fields");
        cut.Markup.Should().Contain("Personal Info");
    }

    [Fact]
    public void AudioPlayer_TogglesPlaybackAndFormatsTime()
    {
        bool playing = false;
        var cut = RenderComponent<AudioPlayer>(p => p
            .Add(x => x.Title, "Podcast Ep 1")
            .Add(x => x.Duration, 120.0)
            .Add(x => x.IsPlaying, playing)
            .Add(x => x.IsPlayingChanged, EventCallback.Factory.Create<bool>(this, p => playing = p)));

        cut.Find(".tento-audio-player").Should().NotBeNull();
        cut.Markup.Should().Contain("Podcast Ep 1");
        AudioPlayer.FormatTime(125).Should().Be("2:05");
        AudioPlayer.FormatTime(3665).Should().Be("1:01:05");

        // Click play button
        cut.Find("button[aria-label='Play']").Click();
        playing.Should().BeTrue();
    }

    [Fact]
    public void AudioPlayer_SeeksAndSkips()
    {
        double currentTime = 30.0;
        var cut = RenderComponent<AudioPlayer>(p => p
            .Add(x => x.Duration, 100.0)
            .Add(x => x.CurrentTime, currentTime)
            .Add(x => x.CurrentTimeChanged, EventCallback.Factory.Create<double>(this, t => currentTime = t)));

        // Skip forward +10s
        cut.Find("button[aria-label='Skip forward 10s']").Click();
        currentTime.Should().Be(40.0);

        // Skip backward -10s
        cut.Find("button[aria-label='Skip backward 10s']").Click();
        currentTime.Should().Be(30.0);
    }

    [Fact]
    public void Voiceform_CalculatesActiveBarsAndSeeks()
    {
        double currentTime = 0.0;
        var cut = RenderComponent<Voiceform>(p => p
            .Add(x => x.Duration, 100.0)
            .Add(x => x.CurrentTime, currentTime)
            .Add(x => x.CurrentTimeChanged, EventCallback.Factory.Create<double>(this, t => currentTime = t)));

        cut.Find(".tento-voiceform").Should().NotBeNull();

        // Waveform bars are rendered
        var bars = cut.FindAll(".tento-voiceform [role='slider'] > div");
        bars.Should().NotBeEmpty();

        // Click middle bar
        bars[bars.Count / 2].Click();
        currentTime.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Voiceform_RendersRecordingMode()
    {
        var cut = RenderComponent<Voiceform>(p => p
            .Add(x => x.IsRecording, true)
            .Add(x => x.Variant, VoiceformVariant.Recording));

        cut.Markup.Should().Contain("Recording audio...");
        cut.Find(".animate-ping").Should().NotBeNull();
    }

    [Fact]
    public void Voiceform_StopRecordingButton_StopsRecording()
    {
        bool isRecording = true;
        bool stopped = false;
        var cut = RenderComponent<Voiceform>(p => p
            .Add(x => x.IsRecording, isRecording)
            .Add(x => x.IsRecordingChanged, EventCallback.Factory.Create<bool>(this, r => isRecording = r))
            .Add(x => x.OnStopRecording, EventCallback.Factory.Create(this, () => stopped = true)));

        cut.Find("button[aria-label='Stop recording']").Click();
        isRecording.Should().BeFalse();
        stopped.Should().BeTrue();
    }
}
