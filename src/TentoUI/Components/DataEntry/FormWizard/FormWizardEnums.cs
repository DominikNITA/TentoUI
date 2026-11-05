namespace TentoUI;

/// <summary>
/// Visual presentation style for the step navigation header in FormWizard.
/// </summary>
public enum WizardHeaderType
{
    /// <summary>
    /// Connected circular step indicators with titles and descriptions.
    /// </summary>
    Steps,

    /// <summary>
    /// Pill badges indicating the active step.
    /// </summary>
    Pills,

    /// <summary>
    /// Compact progress bar with step count text.
    /// </summary>
    Progress
}

/// <summary>
/// Status state of an individual step in the wizard.
/// </summary>
public enum WizardStepStatus
{
    Wait,
    Process,
    Finish,
    Error
}

/// <summary>
/// Event arguments delivered when navigating between steps in FormWizard.
/// </summary>
public class WizardStepChangeArgs
{
    /// <summary>
    /// Zero-based index of the current active step.
    /// </summary>
    public int FromIndex { get; set; }

    /// <summary>
    /// Zero-based index of the target step being navigated to.
    /// </summary>
    public int ToIndex { get; set; }

    /// <summary>
    /// Set to true inside BeforeStepChange to cancel navigation (e.g. if validation fails).
    /// </summary>
    public bool Cancel { get; set; }

    public WizardStepChangeArgs(int fromIndex, int toIndex)
    {
        FromIndex = fromIndex;
        ToIndex = toIndex;
    }
}
