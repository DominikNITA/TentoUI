namespace TentoUI;

[Since("1.1.0")]
public enum DiffLineType
{
    Unchanged,
    Added,
    Removed
}

[Since("1.1.0")]
public sealed record DiffLine(
    DiffLineType Type,
    int? OldLineNumber,
    int? NewLineNumber,
    string? OldContent,
    string? NewContent);
