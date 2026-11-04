global using TentoUI.Extensibility;

namespace TentoUI.Extensibility;

/// <summary>
/// Indicates the TentoUI release version in which a component or parameter was introduced.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false, Inherited = true)]
public sealed class SinceAttribute : Attribute
{
    public string Version { get; }
    public string? Notes { get; set; }

    public SinceAttribute(string version)
    {
        Version = version;
    }
}
