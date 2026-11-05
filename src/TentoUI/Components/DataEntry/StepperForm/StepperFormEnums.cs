namespace TentoUI;

/// <summary>
/// Layout orientation for the StepperForm component.
/// </summary>
public enum StepperFormLayout
{
    /// <summary>
    /// Stepper track placed horizontally above the form card.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Stepper track placed vertically on the left side with form fields on the right.
    /// </summary>
    Vertical
}

/// <summary>
/// Internal interface for step registration within StepperForm.
/// </summary>
public interface IStepperFormContext
{
    void RegisterStep(StepperFormStep step);
    void UnregisterStep(StepperFormStep step);
}
