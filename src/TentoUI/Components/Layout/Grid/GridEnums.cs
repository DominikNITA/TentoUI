namespace TentoUI;

public enum RowJustify
{
    Start,
    End,
    Center,
    SpaceAround,
    SpaceBetween,
    SpaceEvenly
}

public enum RowAlign
{
    Top,
    Middle,
    Bottom,
    Stretch
}

public class RowContext
{
    public int HorizontalGutter { get; set; }
    public int VerticalGutter { get; set; }
}

public class ColSpanSettings
{
    public int? Span { get; set; }
    public int? Offset { get; set; }
    public int? Order { get; set; }
    public int? Pull { get; set; }
    public int? Push { get; set; }
}
