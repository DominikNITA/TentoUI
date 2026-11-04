namespace TentoUI;

public class TourStepInfo
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TargetSelector { get; set; }
}

public class TourTargetRect
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double WindowWidth { get; set; }
    public double WindowHeight { get; set; }
}
