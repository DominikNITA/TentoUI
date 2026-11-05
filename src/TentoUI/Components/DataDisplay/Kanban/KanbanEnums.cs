namespace TentoUI;

/// <summary>
/// Defines a column configuration in a KanbanBoard.
/// </summary>
/// <param name="Key">Unique identifier for the column matching card ColumnKey.</param>
/// <param name="Title">Human-readable column title displayed in the column header.</param>
/// <param name="MaxItems">Optional Work-In-Progress (WIP) capacity limit.</param>
/// <param name="BadgeColor">Color variant applied to the count badge.</param>
public record KanbanColumnDefinition(
    string Key,
    string Title,
    int? MaxItems = null,
    TagColor BadgeColor = TagColor.Default);

/// <summary>
/// Standard card data model for KanbanBoard when not using a custom data class.
/// </summary>
public class KanbanCardItem
{
    /// <summary>
    /// Unique card identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Title header of the card.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Secondary descriptive text or notes.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Key of the column that currently contains this card.
    /// </summary>
    public string ColumnKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional tag text badge.
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// Color variant applied to the tag badge.
    /// </summary>
    public TagColor TagColor { get; set; } = TagColor.Primary;

    /// <summary>
    /// Name or initials of the assigned team member.
    /// </summary>
    public string? Assignee { get; set; }

    /// <summary>
    /// Task priority (e.g., High, Medium, Low).
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Tag color variant for the priority label.
    /// </summary>
    public TagColor PriorityColor { get; set; } = TagColor.Default;
}

/// <summary>
/// Event arguments delivered when a card is moved between columns via drag-and-drop or manual actions.
/// </summary>
/// <typeparam name="TItem">Type of the card item data.</typeparam>
/// <param name="Item">The card item that was moved.</param>
/// <param name="SourceColumnKey">The column key where the card originated.</param>
/// <param name="TargetColumnKey">The destination column key.</param>
/// <param name="TargetIndex">Optional zero-based destination index within the target column.</param>
public record KanbanCardMoveArgs<TItem>(
    TItem Item,
    string SourceColumnKey,
    string TargetColumnKey,
    int? TargetIndex = null);
