namespace TentoUI;

public record KanbanColumnDefinition(
    string Key,
    string Title,
    int? MaxItems = null,
    TagColor BadgeColor = TagColor.Default);

public class KanbanCardItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ColumnKey { get; set; } = string.Empty;
    public string? Tag { get; set; }
    public TagColor TagColor { get; set; } = TagColor.Primary;
    public string? Assignee { get; set; }
}

public record KanbanCardMoveArgs<TItem>(
    TItem Item,
    string SourceColumnKey,
    string TargetColumnKey);
