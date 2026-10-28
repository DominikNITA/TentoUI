using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace TentoUI;

public enum RowSelectionMode
{
    None,
    Single,
    Multiple
}

public enum SortDirection
{
    None,
    Asc,
    Desc
}

public enum ColumnFilterMode
{
    Contains,
    StartsWith,
    EndsWith,
    Equals,
    Select,
    Custom
}

public record ColumnFilterOption(string Label, string Value);

public record TableQueryState(
    int PageIndex,
    int PageSize,
    string? SortColumnTitle,
    string? SortColumnKey,
    SortDirection SortDirection,
    IReadOnlyDictionary<string, string> Filters);

public class ColumnFilterContext<TItem>
{
    public ColumnBase<TItem> Column { get; }
    public string? FilterValue { get; }
    public Func<string?, Task> ApplyFilterAsync { get; }
    public Func<Task> ClearFilterAsync { get; }

    public ColumnFilterContext(
        ColumnBase<TItem> column,
        string? filterValue,
        Func<string?, Task> applyFilterAsync,
        Func<Task> clearFilterAsync)
    {
        Column = column;
        FilterValue = filterValue;
        ApplyFilterAsync = applyFilterAsync;
        ClearFilterAsync = clearFilterAsync;
    }
}

public abstract class ColumnBase<TItem> : ComponentBase, IDisposable
{
    [CascadingParameter] protected Table<TItem>? Table { get; set; }

    [Parameter] public string? Key { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public RenderFragment? TitleTemplate { get; set; }
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }
    [Parameter] public RenderFragment? FooterTemplate { get; set; }
    [Parameter] public string? Width { get; set; }
    [Parameter] public string? MinWidth { get; set; }
    [Parameter] public string? MaxWidth { get; set; }
    [Parameter] public Align Align { get; set; } = Align.Left;
    [Parameter] public bool Visible { get; set; } = true;
    [Parameter] public bool Ellipsis { get; set; }
    [Parameter] public string? HeaderClass { get; set; }
    [Parameter] public string? CellClass { get; set; }
    [Parameter] public string? Format { get; set; }

    // Custom cell display
    [Parameter] public RenderFragment<TItem>? CellTemplate { get; set; }
    [Parameter] public RenderFragment<TItem>? ChildContent { get; set; }

    // Sorting configuration
    [Parameter] public bool Sortable { get; set; }
    [Parameter] public SortDirection DefaultSortOrder { get; set; } = SortDirection.None;
    [Parameter] public Func<TItem, object?>? SortBy { get; set; }
    [Parameter] public Comparison<TItem>? CustomSorter { get; set; }

    // Filtering configuration
    [Parameter] public bool Filterable { get; set; }
    [Parameter] public ColumnFilterMode FilterMode { get; set; } = ColumnFilterMode.Contains;
    [Parameter] public string? FilterPlaceholder { get; set; }
    [Parameter] public string? FilterValue { get; set; }
    [Parameter] public EventCallback<string?> FilterValueChanged { get; set; }
    [Parameter] public IEnumerable<ColumnFilterOption>? FilterOptions { get; set; }
    [Parameter] public Func<TItem, object?>? FilterBy { get; set; }
    [Parameter] public Func<TItem, string, bool>? FilterPredicate { get; set; }
    [Parameter] public RenderFragment<ColumnFilterContext<TItem>>? FilterTemplate { get; set; }

    public string EffectiveKey => !string.IsNullOrWhiteSpace(Key)
        ? Key!
        : (!string.IsNullOrWhiteSpace(Title) ? Title! : GetHashCode().ToString(CultureInfo.InvariantCulture));

    public abstract RenderFragment RenderCell(TItem item);
    public abstract object? GetValue(TItem item);

    public virtual object? GetSortValue(TItem item)
    {
        if (SortBy != null) return SortBy(item);
        return GetValue(item);
    }

    public virtual object? GetFilterValue(TItem item)
    {
        if (FilterBy != null) return FilterBy(item);
        if (SortBy != null) return SortBy(item);
        return GetValue(item);
    }

    public virtual bool MatchesFilter(TItem item, string? filterText)
    {
        if (string.IsNullOrWhiteSpace(filterText)) return true;

        var trimmed = filterText.Trim();
        if (FilterPredicate != null)
        {
            return FilterPredicate(item, trimmed);
        }

        var rawValue = GetFilterValue(item);
        if (rawValue == null) return false;

        var formatted = FormatValue(rawValue);
        if (string.IsNullOrEmpty(formatted)) return false;

        return FilterMode switch
        {
            ColumnFilterMode.StartsWith => formatted.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase),
            ColumnFilterMode.EndsWith => formatted.EndsWith(trimmed, StringComparison.OrdinalIgnoreCase),
            ColumnFilterMode.Equals or ColumnFilterMode.Select => string.Equals(formatted, trimmed, StringComparison.OrdinalIgnoreCase),
            _ => formatted.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
        };
    }

    protected string? FormatValue(object? val)
    {
        if (val == null) return null;
        if (!string.IsNullOrEmpty(Format) && val is IFormattable formattable)
        {
            return formattable.ToString(Format, CultureInfo.CurrentCulture);
        }
        return val.ToString();
    }

    protected override void OnInitialized()
    {
        Table?.AddColumn(this);
    }

    public void Dispose()
    {
        Table?.RemoveColumn(this);
    }
}

public class PropertyColumn<TItem, TProp> : ColumnBase<TItem>
{
    [Parameter, EditorRequired] public Expression<Func<TItem, TProp>> Property { get; set; } = default!;

    private Func<TItem, TProp>? _compiledProperty;
    private Expression<Func<TItem, TProp>>? _lastProperty;

    protected override void OnParametersSet()
    {
        if (Property != null && !ReferenceEquals(Property, _lastProperty))
        {
            _compiledProperty = Property.Compile();
            _lastProperty = Property;

            if (string.IsNullOrEmpty(Title) && Property.Body is MemberExpression memberExpr)
            {
                Title = memberExpr.Member.Name;
            }
        }
        base.OnParametersSet();
    }

    public override object? GetValue(TItem item) => _compiledProperty != null && item != null ? _compiledProperty(item) : null;

    public override RenderFragment RenderCell(TItem item) => builder =>
    {
        var template = CellTemplate ?? ChildContent;
        if (template != null)
        {
            builder.AddContent(0, template(item));
            return;
        }

        var val = GetValue(item);
        builder.AddContent(0, FormatValue(val));
    };
}

public class TemplateColumn<TItem> : ColumnBase<TItem>
{
    [Parameter] public Func<TItem, object?>? Property { get; set; }

    public override object? GetValue(TItem item)
    {
        if (Property != null && item != null) return Property(item);
        if (SortBy != null && item != null) return SortBy(item);
        if (FilterBy != null && item != null) return FilterBy(item);
        return null;
    }

    public override RenderFragment RenderCell(TItem item) => builder =>
    {
        var template = CellTemplate ?? ChildContent;
        if (template != null)
        {
            builder.AddContent(0, template(item));
            return;
        }

        var val = GetValue(item);
        if (val != null)
        {
            builder.AddContent(0, FormatValue(val));
        }
    };
}

/// <summary>
/// Unified manual column component allowing users to define how a column is displayed
/// (via <see cref="Property"/>, <see cref="ColumnBase{TItem}.Format"/>, or custom <see cref="ColumnBase{TItem}.ChildContent"/> / <see cref="ColumnBase{TItem}.CellTemplate"/>)
/// and whether the table is sortable or filterable by this column.
/// </summary>
public class TableColumn<TItem> : TemplateColumn<TItem>
{
}

/// <summary>
/// Shorthand alias for <see cref="TableColumn{TItem}"/>.
/// </summary>
public class Column<TItem> : TableColumn<TItem>
{
}
