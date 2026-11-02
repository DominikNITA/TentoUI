namespace TentoUI.Docs.Services;

public record DocVersion(
    string Version,
    string Label,
    string Path,
    bool IsLatest = false,
    bool IsPreview = false,
    string? ReleaseDate = null,
    string? Notes = null);

public class VersionService
{
    public IReadOnlyList<DocVersion> AvailableVersions { get; } =
    [
        new("1.0.0", "v1.0.0", "", IsLatest: true, ReleaseDate: "October 2026", Notes: "Production General Availability (Current)"),
        new("1.1.0-preview", "v1.1.0 (Next)", "preview/", IsPreview: true, ReleaseDate: "November 2026", Notes: "Next release with expanded layout presets"),
        new("0.9.0", "v0.9.0", "v0.9/", ReleaseDate: "September 2026", Notes: "Initial release (Legacy)")
    ];

    public DocVersion CurrentVersion { get; private set; }

    public event Action? OnVersionChanged;

    public VersionService()
    {
        CurrentVersion = AvailableVersions.First(v => v.IsLatest);
    }

    public void SetVersion(DocVersion version)
    {
        if (CurrentVersion != version)
        {
            CurrentVersion = version;
            OnVersionChanged?.Invoke();
        }
    }
}
