namespace TentoUI;

/// <summary>
/// Suggestion item for AutoComplete input.
/// </summary>
/// <param name="Value">The actual text value filled into the input when selected.</param>
/// <param name="Label">Optional visual display label rendered in the dropdown (defaults to Value).</param>
/// <param name="Data">Optional arbitrary payload or underlying domain model instance.</param>
public record AutoCompleteOption(string Value, string? Label = null, object? Data = null);
