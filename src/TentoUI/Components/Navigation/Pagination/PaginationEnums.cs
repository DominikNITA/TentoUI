namespace TentoUI;

public enum PaginationPosition
{
    BottomRight,
    BottomLeft,
    BottomCenter,
    TopRight,
    TopLeft,
    TopCenter,
    Both
}

public record PaginationRangeContext(int StartItem, int EndItem, int TotalItems);
