using Microsoft.AspNetCore.Components.Forms;

namespace TentoUI;

public enum FormLayout
{
    Horizontal,
    Vertical,
    Inline
}

public class FormContext
{
    public EditContext? EditContext { get; set; }
    public FormLayout Layout { get; set; } = FormLayout.Horizontal;
    public string? LabelColClass { get; set; } = "w-full sm:w-1/4 sm:text-right pr-4";
    public string? WrapperColClass { get; set; } = "w-full sm:w-3/4";
    public bool Disabled { get; set; }
}

public class FormItemContext
{
    public string Id { get; set; } = string.Empty;
    public string? Label { get; set; }
    public ComponentStatus ValidateStatus { get; set; } = ComponentStatus.None;
}
