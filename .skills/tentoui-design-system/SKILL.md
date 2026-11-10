---
name: tentoui-design-system
description: Authoritative guide for translating DESIGN.md design tokens into TentoUI Blazor themes and building reusable domain component extensions.
framework: net10.0
metadata:
  tags: [blazor, tentoui, theming, design-md, components, wcag]
---

# TentoUI Design System & Component Extension Skill

Authoritative guide for translating `DESIGN.md` design tokens into TentoUI Blazor themes and architecting reusable domain component extensions.

## Use this skill when

- Reading or updating a project's `DESIGN.md` tokens (colors, typography, radii, dark mode variants).
- Extending TentoUI components (`Card`, `Button`, `Table`, `Input`, `Modal`, `Statistic`) to establish a consistent app-wide brand design system.
- Generating new pages, features, and dashboards that must strictly follow the project's visual and architectural guidelines.
- Enforcing full WCAG 2.1 AA accessibility standards across custom application wrappers.

## Do not use this skill when

- Configuring base Tailwind CSS v4 pipeline or MSBuild asset synchronization (use `tentoui-tailwind-setup`).
- Implementing raw backend CQRS, database models, or API endpoints.

---

## Invariant Rules for AI Agents

### 1. Read DESIGN.md First
- Always locate and inspect `DESIGN.md` in the workspace root before implementing UI components.
- Extract design tokens:
  - **Primary & Accent Colors**: Light brand color, dark mode brand color.
  - **Typography**: Brand font family, heading weights, base body sizes.
  - **Border Radius**: Small (`4px`), Medium (`8px`), Large (`12px`), Full (`9999px`).
  - **Surface & Dark Variants**: Slate, Neutral, Zinc palettes.
- Apply tokens through `builder.Services.AddTentoUI(...)` in `Program.cs` and `<TentoConfigProvider SyncBodyTheme="true">` in `MainLayout.razor` (or `App.razor`).

### 2. Never Recreate Raw HTML Containers
- **STRICT PROHIBITION**: Do **NOT** output `<div class="rounded-lg shadow bg-white dark:bg-slate-900 p-6 ...">` when `TentoUI.Card` exists.
- Always reuse or extend `TentoUI.Card`:
  - **Pattern A (Component Wrapping)**: Create a Razor component inheriting `TentoComponentBase` wrapping `<Card>`.
  - **Pattern B (C# Inheritance)**: Subclass directly (`public class BrandCard : TentoUI.Card`).
- Apply the same rule across the library: use `TentoUI.Button`, `TentoUI.Table`, `TentoUI.Tag`, `TentoUI.Badge`, `TentoUI.Modal`, `TentoUI.Statistic`, and `TentoUI.Descriptions` instead of bespoke HTML primitives.

### 3. Capture & Forward Attributes
- Every extended component **MUST** capture unmatched values and forward them to the underlying element or TentoUI component:
  ```csharp
  [Parameter(CaptureUnmatchedValues = true)]
  public Dictionary<string, object>? AdditionalAttributes { get; set; }
  ```
- In the template markup, attach `@attributes="AdditionalAttributes"`.
- Alternatively, inherit from `TentoComponentBase`, which provides built-in support for `AdditionalAttributes`, `Class`, `Style`, `ResolvedTheme`, and `ResolvedSize`.
- This guarantees preservation of accessibility attributes (`aria-*`, `role`), test IDs (`data-testid`), and consumer utility classes.

### 4. Service Injection via Interfaces
- Never inject concrete service classes. Always inject interface abstractions:
  - `@inject IMessageService MessageService`
  - `@inject IModalService ModalService`
  - `@inject INotificationService NotificationService`

### 5. Input Form Controls
- **CRITICAL**: Always use `<NumberInput>` instead of `<InputNumber>` to prevent compiler ambiguity and naming collisions with Microsoft's built-in `Microsoft.AspNetCore.Components.Forms.InputNumber`.

---

## Token Mapping Architecture (`DESIGN.md` -> `TentoTheme`)

### Sample `DESIGN.md` Token Source

```markdown
# Visual Design System Tokens

- **Brand Primary**: `#2563eb` (Blue 600)
- **Dark Brand Primary**: `#3b82f6` (Blue 500)
- **Success Color**: `#10b981` (Emerald 500)
- **Warning Color**: `#f59e0b` (Amber 500)
- **Error Color**: `#ef4444` (Red 500)
- **Border Radius**: `8px`
- **Font Family**: `'Inter', system-ui, sans-serif`
```

### 1. Registering in `Program.cs`

Configure `TentoTheme` during dependency injection setup:

```csharp
using TentoUI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register TentoUI with design system tokens
builder.Services.AddTentoUI(theme =>
{
    theme.PrimaryColor = "#2563eb";
    theme.DarkPrimaryColor = "#3b82f6";
    theme.SuccessColor = "#10b981";
    theme.WarningColor = "#f59e0b";
    theme.ErrorColor = "#ef4444";
    theme.BorderRadius = "8px";
    theme.FontFamily = "'Inter', system-ui, sans-serif";
});
```

### 2. Provider Hierarchy in `MainLayout.razor`

Wrap the application in `TentoConfigProvider` to synchronize runtime CSS variables and body theme:

```razor
@inherits LayoutComponentBase
@using TentoUI

<TentoConfigProvider SyncBodyTheme="true" InheritFontFamily="true">
    <div class="min-h-screen bg-slate-50 dark:bg-slate-950 text-slate-800 dark:text-slate-100 flex flex-col">
        <header class="border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 px-6 py-4">
            <!-- App Header Navigation -->
        </header>

        <main class="flex-1 p-6 max-w-7xl mx-auto w-full">
            @Body
        </main>
    </div>
</TentoConfigProvider>
```

---

## Reusable Component Extension Patterns

### Pattern A: Wrapper Component (Preferred for Domain Feature Cards)

Create domain components (e.g. `MetricCard.razor`) that standardize headers, badges, and layout while exposing standard slots.

```razor
@* Shared/Components/MetricCard.razor *@
@namespace MyProject.Shared.Components
@inherits TentoComponentBase

<Card Class="@ComputedCardClasses"
      BodyClass="@BodyClass"
      @attributes="AdditionalAttributes">
    <div class="space-y-3">
        <div class="flex items-center justify-between gap-2">
            <div>
                <span class="text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-400 block">
                    @Title
                </span>
                @if (!string.IsNullOrWhiteSpace(Subtitle))
                {
                    <span class="text-[11px] text-slate-600 dark:text-slate-400">@Subtitle</span>
                }
            </div>

            @if (HeaderAction != null)
            {
                <div>@HeaderAction</div>
            }
        </div>

        <div class="text-2xl font-black text-slate-900 dark:text-white tracking-tight">
            @ChildContent
        </div>

        @if (FooterContent != null)
        {
            <div class="pt-2 border-t border-slate-100 dark:border-slate-800 text-xs text-slate-600 dark:text-slate-400 flex items-center justify-between">
                @FooterContent
            </div>
        }
    </div>
</Card>

@code {
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public string? BodyClass { get; set; }
    [Parameter] public RenderFragment? HeaderAction { get; set; }
    [Parameter] public RenderFragment? FooterContent { get; set; }

    private string ComputedCardClasses => CssBuilder.Default("shadow-xs hover:border-primary-400 transition-colors")
        .AddClass(Class)
        .Build();
}
```

### Pattern B: C# Subclassing (Specialized Actions & Controls)

Subclass base components for reusable presets:

```csharp
// Shared/Components/PrimaryConfirmButton.cs
using Microsoft.AspNetCore.Components;
using TentoUI;

namespace MyProject.Shared.Components;

public class PrimaryConfirmButton : Button
{
    public PrimaryConfirmButton()
    {
        Type = ButtonType.Primary;
        Size = ComponentSize.Middle;
    }

    [Parameter]
    public string ConfirmMessage { get; set; } = "Action completed successfully!";

    [Inject]
    public IMessageService MessageService { get; set; } = default!;

    protected override async Task OnClickAsync()
    {
        await base.OnClickAsync();
        MessageService.Success(ConfirmMessage);
    }
}
```

### Pattern C: Domain Form Control (Label + Input + Validation)

Standardize form items with collision-free `<NumberInput>`:

```razor
@* Shared/Components/CurrencyAmountInput.razor *@
@namespace MyProject.Shared.Components

<FormItem Label="@Label" Required="@Required">
    <NumberInput TValue="decimal?"
                 @bind-Value="Value"
                 Min="0"
                 Step="0.01"
                 Placeholder="@Placeholder"
                 Class="w-full">
        <Prefix>
            <span class="text-xs text-slate-600 dark:text-slate-400 font-semibold">$</span>
        </Prefix>
    </NumberInput>
</FormItem>

@code {
    [Parameter] public string Label { get; set; } = "Amount";
    [Parameter] public bool Required { get; set; }
    [Parameter] public string Placeholder { get; set; } = "0.00";
    [Parameter] public decimal? Value { get; set; }
    [Parameter] public EventCallback<decimal?> ValueChanged { get; set; }
}
```

---

## WCAG 2.1 AA Accessibility Invariants

1. **Color Contrast ($\ge 4.5:1$)**:
   - Never use bare `text-slate-400` on light backgrounds (`bg-white` or `bg-slate-50`).
   - Use `text-slate-600 dark:text-slate-400` for secondary and muted content on light surfaces.
2. **Accessible Names on Controls**:
   - Interactive icon buttons and switches must include an accessible name via `AriaLabel="Toggle status"`.
   - Progress bars and gauges must supply `aria-label="Progress"` or contextually relevant descriptions.
3. **Modal Dialogs**:
   - Utilize `<Modal>` directly; keyboard focus trapping and Escape key dismissal run automatically via TentoUI JSInterop.
4. **Form Association**:
   - Every input field must reside inside a `<FormItem Label="...">` or supply an explicit `aria-label`.

---

## Complete Page Scaffold Example

```razor
@page "/dashboard/finance"
@inject IMessageService MessageService

<PageTitle>Finance Overview - MyProject</PageTitle>

<div class="space-y-6">
    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
            <h1 class="text-2xl font-black text-slate-900 dark:text-white tracking-tight">Financial Operations</h1>
            <p class="text-xs text-slate-600 dark:text-slate-400 mt-1">Live metrics and invoice reconciliation</p>
        </div>

        <div class="flex items-center gap-2">
            <Button Type="ButtonType.Default" OnClick="@(() => MessageService.Info("Exporting report..."))">
                <Icon Name="@IconNames.Download" Size="14" />
                <span>Export CSV</span>
            </Button>
            <Button Type="ButtonType.Primary" OnClick="@(() => MessageService.Success("Sync completed!"))">
                <Icon Name="@IconNames.RefreshCw" Size="14" />
                <span>Sync Balances</span>
            </Button>
        </div>
    </div>

    <!-- Metric Cards Grid using TentoUI.Card extension -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricCard Title="Total Invoiced" Subtitle="Current Fiscal Month">
            <Statistic Value="128450" PrefixText="$" Precision="0" />
            <span slot="FooterContent">
                <span>+12.4% vs benchmark</span>
                <Tag Color="green">+12.4%</Tag>
            </span>
        </MetricCard>

        <MetricCard Title="Pending Review" Subtitle="Awaiting Approval">
            <Statistic Value="14" />
            <span slot="FooterContent">
                <span>Requires finance lead sign-off</span>
            </span>
        </MetricCard>
    </div>
</div>
```

