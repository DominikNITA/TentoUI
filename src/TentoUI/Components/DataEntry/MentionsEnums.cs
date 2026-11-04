namespace TentoUI;

public record MentionOption(
    string Value,
    string? Label = null,
    string? Avatar = null,
    string? Description = null,
    bool Disabled = false)
{
    public string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Value : Label;
}
