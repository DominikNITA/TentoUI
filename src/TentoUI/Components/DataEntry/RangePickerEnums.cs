namespace TentoUI;

public record DateRange(DateTime? Start, DateTime? End);

public record DateRangePreset(string Label, DateTime Start, DateTime End);

public record TimeRange(TimeOnly? Start, TimeOnly? End);
