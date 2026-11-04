using System.Reflection;
using Microsoft.AspNetCore.Components;
using TentoUI.Docs.Components;
using TentoUI.Extensibility;

namespace TentoUI.Docs.Services;

/// <summary>
/// Reflects <see cref="ParameterAttribute"/>, <see cref="SinceAttribute"/>, and <see cref="ObsoleteAttribute"/>
/// metadata directly from TentoUI component types to prevent manual API documentation drift.
/// </summary>
public static class ComponentApiRegistry
{
    public static IReadOnlyList<ApiTable.ApiProp> GetProperties(
        Type? componentType,
        IEnumerable<ApiTable.ApiProp>? explicitProperties = null)
    {
        var explicitList = explicitProperties?.ToList() ?? [];
        if (componentType == null)
        {
            return explicitList;
        }

        var explicitByProp = explicitList.ToDictionary(p => p.Property, p => p, StringComparer.OrdinalIgnoreCase);
        var classSince = componentType.GetCustomAttribute<SinceAttribute>()?.Version;

        object? defaultInstance = null;
        if (!componentType.IsAbstract && !componentType.ContainsGenericParameters)
        {
            try
            {
                defaultInstance = Activator.CreateInstance(componentType);
            }
            catch
            {
                // Ignore if no parameterless constructor
            }
        }

        var reflected = new List<ApiTable.ApiProp>();
        var props = componentType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null &&
                        !string.Equals(p.Name, "AdditionalAttributes", StringComparison.Ordinal))
            .OrderBy(p => p.DeclaringType == componentType ? 0 : 1)
            .ThenBy(p => p.Name);

        foreach (var prop in props)
        {
            // Skip companion FooChanged EventCallback properties if Foo already exists
            if (prop.Name.EndsWith("Changed", StringComparison.Ordinal) &&
                prop.PropertyType.IsGenericType &&
                prop.PropertyType.GetGenericTypeDefinition() == typeof(EventCallback<>))
            {
                continue;
            }

            var sinceAttr = prop.GetCustomAttribute<SinceAttribute>();
            var obsoleteAttr = prop.GetCustomAttribute<ObsoleteAttribute>();
            var sinceVersion = sinceAttr?.Version ?? classSince;
            var deprecatedNote = obsoleteAttr != null
                ? (string.IsNullOrWhiteSpace(obsoleteAttr.Message) ? "Deprecated" : obsoleteAttr.Message)
                : null;

            if (explicitByProp.TryGetValue(prop.Name, out var existing))
            {
                reflected.Add(existing with
                {
                    Since = existing.Since ?? sinceVersion,
                    Deprecated = existing.Deprecated ?? deprecatedNote
                });
                explicitByProp.Remove(prop.Name);
            }
            else
            {
                var typeName = FormatTypeName(prop.PropertyType);
                var defaultVal = FormatDefaultValue(prop, defaultInstance);
                var desc = GenerateDescription(prop.Name, typeName);
                reflected.Add(new ApiTable.ApiProp(
                    prop.Name,
                    desc,
                    typeName,
                    defaultVal,
                    sinceVersion,
                    deprecatedNote));
            }
        }

        // Append any remaining explicit rows not matched by reflection
        foreach (var remaining in explicitList.Where(p => explicitByProp.ContainsKey(p.Property)))
        {
            reflected.Add(remaining with { Since = remaining.Since ?? classSince });
        }

        return reflected;
    }

    public static string FormatTypeName(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            return $"{FormatTypeName(underlying)}?";
        }

        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(int)) return "int";
        if (type == typeof(double)) return "double";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(object)) return "object";

        if (type.IsGenericType)
        {
            var baseName = type.Name.Split('`')[0];
            var args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
            return $"{baseName}<{args}>";
        }

        return type.Name;
    }

    private static string? FormatDefaultValue(PropertyInfo prop, object? instance)
    {
        if (instance == null) return null;
        try
        {
            var val = prop.GetValue(instance);
            if (val == null) return "null";
            if (val is bool b) return b ? "true" : "false";
            if (val is string s) return string.IsNullOrEmpty(s) ? "\"\"" : $"\"{s}\"";
            if (val.GetType().IsEnum) return $"{val.GetType().Name}.{val}";
            if (val is ValueType && !val.GetType().Name.StartsWith("EventCallback", StringComparison.Ordinal))
            {
                return Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        catch { }
        return null;
    }

    private static string GenerateDescription(string name, string typeName) => name switch
    {
        "Class" => "Additional CSS utility classes merged onto the root element.",
        "Style" => "Inline CSS styles applied to the root element.",
        "ChildContent" => "Inner content rendered inside the component.",
        "Disabled" => "Disables user interaction and applies muted styling when true.",
        "Size" => "Controls the component size scale (Small, Middle, Large).",
        "Value" => "Current bound value of the component.",
        "Placeholder" => "Placeholder text displayed when the input is empty.",
        _ => $"Configures the {name} ({typeName}) parameter."
    };
}
