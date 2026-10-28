---
name: tentoui-components
description: TentoUI Blazor components based on Tailwind CSS and AntDesign patterns. Authoritative guide for building, styling, and wrapping TentoUI Blazor components in .NET 10.
framework: net10.0
metadata:
  version: 1.0.0
  author: TentoTemplate
  tags: [blazor, tentoui, tailwindcss, wcag, ui]
---

# TentoUI Components Skill

Authoritative guide for building, styling, and wrapping TentoUI Blazor components based on Tailwind CSS and AntDesign patterns.

## Use this skill when

- Developing frontend UI in TentoTemplate Blazor applications (.NET 10).
- Building responsive pages, forms, data tables, dialogs, alerts, and navigation menus.
- Creating reusable UI wrappers or compound design system components on top of TentoUI.
- Implementing dark mode, custom color themes, and CSS custom property token cascade (`--tento-*`).
- Ensuring full WCAG 2.1 AA accessibility compliance (keyboard navigation, ARIA attributes, focus trapping).
- Writing bUnit / xUnit automated component tests for UI interactions and state bindings.

## Do not use this skill when

- Writing backend CQRS domain entities, commands, queries, or database migrations (use `template-architecture-cqrs`).
- Configuring authentication, user identity tokens, or role authorization handlers (use `module-auth-users`).
- Implementing payment gateways such as Stripe or PayU (use `module-payments`).
- Generating PDF documents or QR code barcodes (use `module-documents-qr`).
- Modifying raw server infrastructure, Dockerfiles, or CI/CD pipelines.

## Core Architecture & Rules

1. **Framework Target**: .NET 10 (`net10.0`) with `Microsoft.AspNetCore.Components.Web` and `Microsoft.AspNetCore.Components.Forms`.
2. **Tailwind CSS Utility Foundations**:
   - Zero inline hex colors (`style="color: #1677ff"` is strictly prohibited).
   - All styling MUST be achieved via semantic Tailwind CSS utility classes (`text-primary-600`, `bg-white dark:bg-slate-900`, `border-slate-200 dark:border-slate-800`).
   - Use `CssBuilder` and `StyleBuilder` for conditional class and style composition.
3. **Theme and Design Tokens**:
   - Central theme tokens are configured via `TentoTheme` and rendered by `TentoConfigProvider`.
   - The CSS token cascade emits 14 core custom properties:
     - `--tento-bg-app`, `--tento-bg-surface`, `--tento-bg-subtle`
     - `--tento-text-base`, `--tento-text-muted`, `--tento-text-inverse`
     - `--tento-border-base`
     - `--tento-primary-500`, `--tento-primary-600`
     - `--tento-success`, `--tento-warning`, `--tento-error`, `--tento-info`
     - `--tento-radius-md`, `--tento-font-family`
4. **Component Base Model**:
   - All visual components inherit from `TentoComponentBase` (providing `Class`, `Style`, `AdditionalAttributes`, `ConfigContext`, `ResolvedTheme`, `CurrentSize`, `ResolvedSize`).
   - All form controls inherit from `TentoInputBase<TValue>` (providing two-way `@bind-Value`, validation status cascade, change events, and disabled states).
5. **Accessibility (WCAG 2.1 AA) Invariants**:
   - Focus indicators MUST use `focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500`.
   - Form inputs MUST link to labels via `<label for="{id}">` or provide `aria-label` / `aria-labelledby`.
   - Dialogs MUST have `role="dialog"`, `aria-modal="true"`, `aria-labelledby`, and `aria-describedby`.
   - Dynamic feedback components MUST use `aria-live="polite"` or `role="alert"`.
   - Interactive components MUST handle full keyboard interactions (`Enter`, `Space`, `Escape`, `ArrowUp`, `ArrowDown`).

## API & Component Catalog Reference

### 1. General & Layout
- `Button`: Primary, Default, Dashed, Text, Link variants; `Danger`, `Loading`, `Disabled`, `Size`, `Shape`, `Icon`, `HtmlType`.
- `FloatButton`, `FloatButtonGroup`, `BackTop`: Corner floating action button (FAB) supporting shapes (`Circle`, `Square`), types (`Default`, `Primary`), tooltips, badges, stacked groups, and smooth window scroll-to-top.
- `Typography`: `Title` (Level 1-5), `Text` (Type: Secondary, Success, Warning, Danger, Disabled, Mark, Code, Bold), `Paragraph`, `Link`.
- `Icon`: Embedded Lucide SVGs via `IconNames`, scalable `Size`, color inheritance, `Spin` rotation, accessible `AriaLabel`.
- `Divider`: Horizontal or Vertical, optional title text with `Orientation` (Left, Center, Right), dashed style.
- `Layout`, `Header`, `Sider`, `Content`, `Footer`: Flexbox responsive layout with collapsible sidebar, trigger, and breakpoint listener.
- `Row` & `Col`: 24-column grid system with `Gutter` (horizontal/vertical tuple), `Justify`, `Align`, and responsive span breakpoints (`Xs`, `Sm`, `Md`, `Lg`, `Xl`, `Xxl`).
- `Space`: Direction (`Horizontal`, `Vertical`), `Size` (`Small`, `Middle`, `Large`, or custom px), `Align`, `Wrap`.

### 2. Navigation
- `Menu`: Modes (`Horizontal`, `Vertical`, `Inline`), Themes (`Light`, `Dark`), `SelectedKey`, `OpenKeys`, collapsible sidebar integration.
- `MenuItem`, `SubMenu`, `MenuGroup`, `MenuDivider`: Hierarchical navigation items with icon slots and selection callbacks.
- `Breadcrumb` & `BreadcrumbItem`: Semantic navigation breadcrumbs with customizable separators and route links.
- `Tabs` & `TabPane`: Tab types (`Line`, `Card`), positions (`Top`, `Bottom`, `Left`, `Right`), dynamic tab items, closable tabs.
- `Pagination`: Standalone pagination bar with `PageIndex`, `PageSize`, `TotalItems`, `ShowSizeChanger`, `PageSizeOptions`, `ShowQuickJumper`, `Simple` mode, and smart ellipsis windowing.
- `Steps` & `Step`: Stepper indicator (`Horizontal`, `Vertical`), step statuses (`Wait`, `Process`, `Finish`, `Error`), custom icons, and clickable `OnChange` navigation.
- `Dropdown`: Menu dropdown with `Click` or `Hover` triggers, placements (`BottomLeft`, `BottomRight`, `TopLeft`, etc.), `DropdownMenuItem` list, or custom `Overlay` fragment.
- `Anchor` & `AnchorLink`: Scroll-spy table of contents navigation with active indicator line and smooth target scrolling.
- `Affix`: Sticky pinned viewport container supporting `OffsetTop` and `OffsetBottom`.

### 3. Data Entry
- `Form<TModel>`: Seamless integration with Blazor `EditContext`, `DataAnnotationsValidator`, layouts (`Vertical`, `Horizontal`, `Inline`), unified submission handling.
- `FormItem`: Label, required indicator (`*`), help text, extra description, automatic validation status error display.
- `Input`: Text, password, search, with prefix/suffix icons, clear button (`AllowClear`), size variants.
- `InputNumber<TValue>`: Numeric input with step increment/decrement controls, `Min`, `Max`, `Step`, and prefix slot.
- `AutoComplete`: Text input with real-time suggestion dropdown, custom filter predicates, and keyboard navigation.
- `TimePicker`: Hour, minute, and second selector with popup columns, `HourStep`, `MinuteStep`, `SecondStep`, and quick "Now" selection.
- `ColorPicker`: Color picker with swatch button trigger, preset color palette buttons, native OS color picker, and hex/rgb input.
- `Cascader`: Multi-level cascading dropdown selection for hierarchical categories and regional data.
- `TreeSelect`: Dropdown select with an integrated search-filtered tree view.
- `Transfer`: Dual-list shuttle selector for transferring items between Source and Target lists with item search and select all.
- `TextArea`: Multi-line text input with `Rows`, `AutoResize`, and character counter.
- `SearchInput`: Search button slot, `OnSearch` callback, enter-key trigger.
- `Select<TItem, TValue>`: Single and multiple selection, search filtering, option templates, clear button, keyboard navigation.
- `Checkbox` & `CheckboxGroup<TValue>`: Checkbox state, indeterminate support, group option arrays or template children.
- `Radio<TValue>` & `RadioGroup<TValue>`: Radio buttons and segmented button toggle groups (`RadioOptionType.Button`).
- `Switch`: High-contrast toggle switch with loading spinner and checked/unchecked labels.
- `Slider`: Range slider with `Min`, `Max`, `Step`, `Marks` dictionary, `ShowValue`, and `TipFormatter`.
- `Rate`: Star rating selector with `Count`, `AllowClear`, `Tooltips`, custom `Character`, and `ReadOnly`/`Disabled` states.
- `DatePicker`: Accessible date input with calendar picker popup, format strings, and keyboard shortcuts.
- `Upload`: File selector and drag-and-drop dropzone (`DragDrop="true"`) wrapping Blazor `InputFile` with file list management and size validation.

### 4. Data Display
- `Card` & `CardGrid`: Structured container with title, extra action slot, cover image, and card grid layouts.
- `Table<TItem>`: Customizable data table with manual column definitions (`TableColumn<TItem>`, `Column<TItem>`, `PropertyColumn<TItem, TProp>`, `TemplateColumn<TItem>`), custom cell templates (`CellTemplate` / `ChildContent`) and `Format` strings, per-column sorting (`Sortable`, `SortBy`, `CustomSorter`, `DefaultSortOrder`), per-column filtering (`Filterable`, `FilterMode`, `FilterOptions`, `FilterBy`, `FilterPredicate`, `FilterTemplate`), pagination (`ShowSizeChanger`, `PageSizeOptions`, `ShowQuickJumper`, `TotalCount`, `OnQueryChange`), expandable rows (`ExpandedRowTemplate`), and row selection.
- `Segmented`: Pill segmented control with smooth selection state, icons, disabled items, and block full-width layout.
- `Carousel` & `CarouselPanel`: Banner slideshow with autoplay, autoplay speed, dot navigation positions, and next/prev arrows.
- `Image`: Image viewer with hover preview mask and full-screen preview modal supporting Zoom In/Out, Rotate, and Close.
- `Tree` & `TreeNode`: Hierarchical collapsible tree view with checkable nodes, folder/file icons, and real-time search filtering.
- `Calendar`: Full-month calendar grid view with month/year navigation, weekday headers, and custom `DateCellTemplate`.
- `QRCode`: SVG QR code display with size, color, background, and loading/expired status states with refresh callback.
- `Watermark`: Security watermark overlay pattern wrapping content with custom text, rotation, font, and spacing gaps.
- `Tag`: Color variants (default, success, warning, error, info, custom hex), closable tags with `OnClose` callback.
- `Badge`: Numeric count badges, overflow ceiling (`OverflowCount="99"`), status indicator dots (`Success`, `Processing`, `Default`, `Error`, `Warning`).
- `Avatar` & `AvatarGroup`: User avatars supporting image `Src` (with automatic fallback), `Icon`, or `Text` initials in `Circle` or `Square` shapes.
- `Collapse` & `CollapsePanel`: Accordion and multi-panel disclosure containers with custom headers and `Extra` slots.
- `Descriptions` & `DescriptionsItem`: Multi-column key/value detail grids with `Bordered` table layout and column `Span` control.
- `Statistic`: KPI metric display with `Title`, `Value`, `Precision` formatting, `Prefix`, and `Suffix`.
- `Timeline` & `TimelineItem`: Vertical chronological event feed with semantic dot colors and custom `Dot` templates.
- `Popover`: Floating rich content card triggered by `Click` or `Hover`.
- `Empty`: Empty state placeholder with customizable icon/image, description, and footer action slot.
- `List<TItem>`, `ListItem`, `ListItemMeta`: Item feeds with avatars, titles, descriptions, action slots, and pagination.

### 5. Feedback
- `Modal`: Accessible dialog, focus trap, escape key dismiss, backdrop click toggle, header, footer, `ConfirmLoading`.
- `IModalService`: Programmatic confirm dialogs: `modalService.ConfirmAsync(new ModalConfirmOptions { Title = "...", Content = "..." })`.
- `Drawer`: Slide-over panel from `Right`, `Left`, `Top`, or `Bottom` with mask backdrop, header, body, and footer slots.
- `Alert`: Alert banners (`Success`, `Info`, `Warning`, `Error`), description, icon, closable with `OnClose`.
- `IMessageService`: Floating toast feedback: `messageService.Success("Saved successfully")`, `messageService.Error("Failed")`.
- `NotificationHost` & `INotificationService`: Desktop-style rich notifications with Title, Description, Type (`Success`, `Info`, `Warning`, `Error`), Placements (`TopRight`, `TopLeft`, `BottomRight`, `BottomLeft`), and action buttons.
- `Tour` & `TourStep`: Step-by-step guided walkthrough popover system with target element highlighting, step count (`1/3`), and Next/Finish buttons.
- `Progress`: Linear and circular progress bars (`ProgressType.Line`, `ProgressType.Circle`) with status colors and custom `Format`.
- `Skeleton`: Animated loading placeholder with configurable `Avatar`, `Title`, and `Rows`.
- `Popconfirm`: Confirmation popover anchored to a trigger element with `OnConfirm` and `OnCancel` callbacks.
- `Result`: Full-section status feedback (`Success`, `Info`, `Warning`, `Error`) with title, subtitle, and `Extra` action buttons.
- `Spin`: Loading spinner with size variants, container wrapping overlay with `Tip` loading text.
- `Tooltip`: Floating tooltip with placement (`Top`, `Bottom`, `Left`, `Right`), trigger events (`Hover`, `Focus`, `Click`).

## Usage Patterns & Code Examples

### Standard Form with Validation
```razor
@using TentoUI

<Form Model="@_model" OnValidSubmit="@HandleSubmit" Layout="FormLayout.Vertical">
    <FormItem Label="Email Address" Required="true" For="@(() => _model.Email)">
        <Input @bind-Value="_model.Email" Placeholder="admin@example.com" AllowClear="true">
            <Prefix><Icon Name="IconNames.Mail" Size="16" /></Prefix>
        </Input>
    </FormItem>

    <FormItem Label="Role" Required="true" For="@(() => _model.RoleId)">
        <Select TItem="RoleItem" TValue="int"
                DataSource="@_roles"
                ValueProperty="@(r => r.Id)"
                LabelProperty="@(r => r.Name)"
                @bind-Value="_model.RoleId"
                Placeholder="Select role" />
    </FormItem>

    <div class="flex justify-end gap-2 mt-4">
        <Button HtmlType="ButtonHtmlType.Submit" Type="ButtonType.Primary" Loading="@_isSubmitting">
            Save Changes
        </Button>
    </div>
</Form>

@code {
    private UserModel _model = new();
    private bool _isSubmitting;
    private List<RoleItem> _roles = new() { new(1, "Administrator"), new(2, "Editor") };

    private async Task HandleSubmit()
    {
        _isSubmitting = true;
        // submit logic
        _isSubmitting = false;
    }

    public record RoleItem(int Id, string Name);
    public class UserModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
    }
}
```

### Customizable Table with Manual Columns, Filtering, Sorting, and Pagination
```razor
@using TentoUI

<Table TItem="UserDto"
       DataSource="@_users"
       RowSelection="RowSelectionMode.Multiple"
       @bind-SelectedRows="_selectedUsers"
       @bind-PageIndex="_pageIndex"
       @bind-PageSize="_pageSize"
       ShowSizeChanger="true"
       ShowQuickJumper="true"
       Striped="true">
    <PropertyColumn TItem="UserDto" TProp="int"
                    Property="@(u => u.Id)"
                    Title="ID"
                    Width="80px"
                    Sortable="true" />

    <TableColumn TItem="UserDto"
                 Title="Employee"
                 Property="@(u => u.Name)"
                 Sortable="true"
                 Filterable="true"
                 FilterPlaceholder="Search name..."
                 Context="user">
        <div class="flex items-center gap-2">
            <Avatar Text="@user.Name[..1]" Size="ComponentSize.Small" />
            <span class="font-medium">@user.Name</span>
        </div>
    </TableColumn>

    <TableColumn TItem="UserDto"
                 Title="Role"
                 Property="@(u => u.Role)"
                 Sortable="true"
                 Filterable="true"
                 FilterMode="ColumnFilterMode.Select"
                 FilterOptions="@_roleFilterOptions" />

    <TemplateColumn TItem="UserDto"
                    Title="Status"
                    Sortable="true"
                    SortBy="@(u => u.IsActive)"
                    Filterable="true"
                    FilterBy="@(u => u.IsActive ? "Active" : "Inactive")">
        <Tag Color="@(context.IsActive ? TagColor.Success : TagColor.Default)">
            @(context.IsActive ? "Active" : "Inactive")
        </Tag>
    </TemplateColumn>
</Table>

@code {
    private int _pageIndex = 1;
    private int _pageSize = 10;
    private List<UserDto> _users = new();
    private HashSet<UserDto> _selectedUsers = new();
    private readonly ColumnFilterOption[] _roleFilterOptions =
    [
        new("Admin", "Admin"),
        new("Editor", "Editor"),
        new("Viewer", "Viewer")
    ];

    public record UserDto(int Id, string Name, string Role, bool IsActive);
}
```

### Component Wrapping & Extensibility Pattern
When wrapping TentoUI components into domain-specific building blocks:
1. Inherit from `TentoComponentBase` or `TentoInputBase<TValue>`.
2. Forward unmatched attributes to the root element with `@attributes="AdditionalAttributes"`.
3. Compose styling using `CssBuilder` to merge base classes with user-provided `Class`.
4. Provide appropriate default ARIA attributes.

```razor
@namespace MyCompany.Components
@inherits TentoComponentBase

<div class="@ComputedClass" style="@Style" @attributes="AdditionalAttributes">
    <Card Title="@Title">
        <Extra>
            @if (Closable)
            {
                <Button Type="ButtonType.Text" Size="ComponentSize.Small" OnClick="@OnClose">
                    <Icon Name="IconNames.X" Size="14" />
                </Button>
            }
        </Extra>
        @ChildContent
    </Card>
</div>

@code {
    [Parameter] public string? Title { get; set; }
    [Parameter] public bool Closable { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string ComputedClass => new CssBuilder("domain-panel transition-shadow hover:shadow-md")
        .AddClass(Class)
        .Build();
}
```

## Accessibility (WCAG 2.1 AA) Guidelines

- **Focus Visibility**: Ensure interactive elements use Tailwind's `focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:outline-none`.
- **Keyboard Navigation**:
  - `Button`: Space / Enter activates click.
  - `Select`: Enter / Space opens dropdown, ArrowUp / ArrowDown traverses options, Enter selects option, Escape closes dropdown.
  - `Modal`: Escape dismisses dialog when `Keyboard="true"`. Tab cycles inside modal container (focus trapping).
  - `RadioGroup`: Arrow keys change selected radio button.
  - `Checkbox`: Spacebar toggles check state.
- **ARIA Attributes**:
  - `aria-expanded` on accordion / submenu triggers.
  - `aria-checked` on checkboxes, radio buttons, and switches.
  - `aria-selected` on active tabs and table rows.
  - `aria-invalid` on form inputs failing validation.
  - `aria-busy="true"` on elements currently in loading state.
  - `aria-modal="true"` on modal dialogs.
  - `aria-label` or `aria-labelledby` linking modals and controls to descriptive labels.

## Verification & Testing Method

Verify TentoUI components using bUnit and xUnit:
```bash
# Build TentoUI library
dotnet build src/TentoUI/TentoUI.csproj

# Run bUnit test suite
dotnet test tests/TentoUI.Tests/TentoUI.Tests.csproj
```

In bUnit test classes:
```csharp
using Bunit;
using Xunit;
using FluentAssertions;
using TentoUI;

public class ButtonTests : TestContext
{
    [Fact]
    public void Button_RendersPrimaryStyle_AndHandlesClicks()
    {
        var clicked = false;
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Type, ButtonType.Primary)
            .Add(p => p.OnClick, () => clicked = true)
            .AddChildContent("Submit"));

        cut.Find("button").ClassList.Should().Contain("tento-btn-primary");
        cut.Find("button").Click();
        clicked.Should().BeTrue();
    }
}
```
