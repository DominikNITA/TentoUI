namespace TentoUI;

public record CommandItem(
    string Key,
    string Title,
    string? Category = null,
    string? Subtitle = null,
    string? Icon = null,
    string? Shortcut = null,
    string? Href = null,
    bool Disabled = false,
    string[]? Keywords = null);
