namespace TentoUI;

[Since("1.0.0")]
public enum GaugeStatus
{
    Default,
    Success,
    Warning,
    Error
}

[Since("1.0.0")]
public sealed record MeterSegment(
    string Label,
    double Value,
    string? ColorClass = null);
