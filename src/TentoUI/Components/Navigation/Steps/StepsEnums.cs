using Microsoft.AspNetCore.Components;

namespace TentoUI;

public enum StepStatus
{
    Wait,
    Process,
    Finish,
    Error
}

public enum StepsDirection
{
    Horizontal,
    Vertical
}

public class StepItemInfo
{
    public string? Title { get; set; }
    public string? SubTitle { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public RenderFragment? IconTemplate { get; set; }
    public StepStatus? Status { get; set; }
    public bool Disabled { get; set; }
}
