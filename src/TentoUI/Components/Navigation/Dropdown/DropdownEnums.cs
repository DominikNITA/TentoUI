namespace TentoUI;

public enum DropdownTrigger
{
    Click,
    Hover
}

public enum DropdownPlacement
{
    BottomLeft,
    BottomRight,
    BottomCenter,
    TopLeft,
    TopRight,
    TopCenter
}

public record DropdownMenuItem(
    string Key,
    string Label,
    string? Icon = null,
    bool Disabled = false,
    bool Danger = false,
    bool DividerBefore = false);
