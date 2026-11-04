namespace TentoUI;

public enum FilterLogicalOperator
{
    And,
    Or
}

public enum FilterComparisonOperator
{
    Contains,
    Equals,
    StartsWith,
    EndsWith,
    GreaterThan,
    LessThan
}

public record FilterFieldDefinition(
    string Key,
    string Label,
    string[]? Options = null);

public class FilterRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FieldKey { get; set; } = string.Empty;
    public FilterComparisonOperator Operator { get; set; } = FilterComparisonOperator.Contains;
    public string Value { get; set; } = string.Empty;
}
