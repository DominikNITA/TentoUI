namespace TentoUI;

/// <summary>
/// Visual presentation variant for AudioPlayer.
/// </summary>
public enum AudioPlayerVariant
{
    /// <summary>
    /// Standard horizontal audio bar with controls, scrubber, and time labels.
    /// </summary>
    Default,

    /// <summary>
    /// Rich card layout featuring cover artwork, title, artist, and full media controls.
    /// </summary>
    Card,

    /// <summary>
    /// Compact single-line bar suitable for chat messages or table cells.
    /// </summary>
    Compact,

    /// <summary>
    /// Minimalist scrubber bar with embedded play/pause toggle.
    /// </summary>
    Minimal
}
