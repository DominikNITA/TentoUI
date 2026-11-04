using Microsoft.AspNetCore.Components;
using TentoUI.Configuration;

namespace TentoUI.Extensibility;

public abstract class TentoComponentBase : ComponentBase
{
    [CascadingParameter] protected TentoConfigContext? ConfigContext { get; set; }

    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    protected string ElementId { get; } = $"tento-{Guid.NewGuid():N}"[..14];
    protected TentoTheme? ResolvedTheme => ConfigContext?.Theme;
    protected ComponentSize CurrentSize => SizeOverride ?? ConfigContext?.ComponentSize ?? ComponentSize.Middle;
    protected virtual ComponentSize? SizeOverride => null;

    protected ComponentSize ResolvedSize(ComponentSize? explicitSize) =>
        explicitSize ?? ConfigContext?.ComponentSize ?? ComponentSize.Middle;
}
