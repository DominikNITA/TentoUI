namespace TentoUI;

/// <summary>
/// Display variant for the Voiceform component.
/// </summary>
public enum VoiceformVariant
{
    /// <summary>
    /// Standard voice message bubble with interactive waveform scrubber and play toggle.
    /// </summary>
    Default,

    /// <summary>
    /// Compact single-line soundwave without author header.
    /// </summary>
    Compact,

    /// <summary>
    /// Live recording indicator with pulsating red dot and animated sound waves.
    /// </summary>
    Recording
}
