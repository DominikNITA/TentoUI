namespace TentoUI;

public record SegmentedOption<TValue>(
    TValue Value,
    string? Label = null,
    string? Icon = null,
    bool Disabled = false,
    Microsoft.AspNetCore.Components.RenderFragment? Template = null);
