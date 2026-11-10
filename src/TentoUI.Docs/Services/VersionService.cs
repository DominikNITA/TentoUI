using System.Net.Http.Json;

namespace TentoUI.Docs.Services;

public record DocVersion(
    string Version,
    string Label,
    string Path,
    bool IsLatest = false,
    bool IsPreview = false,
    string? ReleaseDate = null,
    string? Notes = null,
    bool IsLegacy = false);

public class VersionService
{
    private List<DocVersion> _versions =
    [
        new("1.0.9", "v1.0.9", "/", IsLatest: true, ReleaseDate: "November 2026", Notes: "Current release (Default Purple brand theme, tentoui-design-system AI skill, clean design and component extendability refocus)"),
        new("1.0.8", "v1.0.8", "/v1.0.8/", ReleaseDate: "November 2026", Notes: "Modal focus trap & Escape direct JSInterop, full WCAG 2.1 AA docs contrast remediation, BadgeStatus tokens, Rate half-stars, Statistic affixes"),
        new("1.0.7", "v1.0.7", "/v1.0.7/", ReleaseDate: "November 2026", Notes: "Quality Hardening & WCAG 2.1 AA: Switch/Progress/Gauge a11y, Carousel/Splitter ARIA fixes, BadgeStatus enum, Rate decimal/half-stars, Statistic PrefixText/SuffixText, AddTentoUI theme overload, NumberInput collision alias"),
        new("1.0.6", "v1.0.6", "/v1.0.6/", ReleaseDate: "November 2026", Notes: "Maintenance release"),
        new("1.0.5", "v1.0.5", "/v1.0.5/", ReleaseDate: "November 2026", Notes: "Maintenance release"),
        new("1.0.4", "v1.0.4", "/v1.0.4/", ReleaseDate: "November 2026", Notes: "Responsive layout & Quick Search enhancements"),
        new("1.0.3", "v1.0.3", "/v1.0.3/", ReleaseDate: "November 2026", Notes: "FormWizard, StepperForm, AudioPlayer, Voiceform, and Kanban Drag & Drop"),
        new("1.0.2", "v1.0.2", "/v1.0.2/", ReleaseDate: "November 2026", Notes: "Tour target tracking & spotlight mask, Modal numeric width fixes, Tooltip styling"),
        new("1.0.1", "v1.0.1", "/v1.0.1/", ReleaseDate: "November 2026", Notes: "Icon spin speed, ContextMenu & docs enhancements"),
        new("1.0.0", "v1.0.0", "/v1.0/", ReleaseDate: "October 2026", Notes: "Production General Availability"),
        new("1.1.0-preview", "v1.1.0 (Next)", "/next/", IsPreview: true, ReleaseDate: "November 2026", Notes: "Upcoming feature preview"),
        new("0.9.0", "v0.9.0", "/v0.9/", ReleaseDate: "September 2026", Notes: "Initial release (Legacy)", IsLegacy: true)
    ];

    public IReadOnlyList<DocVersion> AvailableVersions => _versions;

    public DocVersion CurrentVersion { get; private set; }

    public bool IsLoadedFromManifest { get; private set; }

    public event Action? OnVersionChanged;

    public VersionService()
    {
        CurrentVersion = _versions.First(v => v.IsLatest);
    }

    public async Task LoadVersionsAsync(HttpClient? httpClient, string? currentUri = null)
    {
        if (httpClient != null && !IsLoadedFromManifest)
        {
            try
            {
                var fetched = await httpClient.GetFromJsonAsync<List<DocVersion>>("versions.json");
                if (fetched is { Count: > 0 })
                {
                    _versions = fetched;
                    IsLoadedFromManifest = true;
                }
            }
            catch
            {
                // Fallback to built-in versions when running in local dev or offline bUnit tests
            }
        }

        if (!string.IsNullOrWhiteSpace(currentUri))
        {
            DetectVersionFromUri(currentUri);
        }
        else
        {
            CurrentVersion = _versions.FirstOrDefault(v => v.IsLatest) ?? _versions[0];
        }

        OnVersionChanged?.Invoke();
    }

    public void DetectVersionFromUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri)) return;

        foreach (var v in _versions)
        {
            var trimmedPath = v.Path.Trim('/');
            if (!string.IsNullOrEmpty(trimmedPath) &&
                uri.Contains($"/{trimmedPath}/", StringComparison.OrdinalIgnoreCase))
            {
                CurrentVersion = v;
                return;
            }
        }
    }

    public void SetVersion(DocVersion version)
    {
        if (CurrentVersion != version)
        {
            CurrentVersion = version;
            OnVersionChanged?.Invoke();
        }
    }

    public void SwitchToLatest()
    {
        var latest = _versions.FirstOrDefault(v => v.IsLatest) ?? _versions[0];
        SetVersion(latest);
    }

    /// <summary>
    /// Computes the cross-version route preserving the current relative page path.
    /// For example, switching from v1.0 to v1.1 on "components/table" yields "/next/components/table".
    /// </summary>
    public static string GetVersionRoute(DocVersion targetVersion, string currentRelativePath)
    {
        var cleanRel = (currentRelativePath ?? string.Empty).TrimStart('/');

        // Strip any existing version prefix from relative path if present
        string[] knownPrefixes = ["v1.0/", "v1.1/", "v0.9/", "next/", "preview/", "latest/"];
        foreach (var prefix in knownPrefixes)
        {
            if (cleanRel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                cleanRel = cleanRel[prefix.Length..];
                break;
            }
        }

        var basePath = string.IsNullOrWhiteSpace(targetVersion.Path)
            ? "/"
            : $"/{targetVersion.Path.Trim('/')}/";

        return $"{basePath}{cleanRel}";
    }
}
