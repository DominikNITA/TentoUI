using System.Reflection;
using Microsoft.AspNetCore.Components;
using TentoUI.Docs.Components;
using TentoUI.Extensibility;

namespace TentoUI.Docs.Services;

/// <summary>
/// Reflects <see cref="ParameterAttribute"/>, <see cref="SinceAttribute"/>, <see cref="ObsoleteAttribute"/>,
/// and referenced enum/model types directly from TentoUI component types.
/// </summary>
public static class ComponentApiRegistry
{
    private static readonly Dictionary<string, Type> s_tentoTypesByName = typeof(Button).Assembly
        .GetTypes()
        .Where(t => t.IsPublic && !t.IsNested)
        .GroupBy(t => t.Name.Split('`')[0], StringComparer.OrdinalIgnoreCase)
        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    public static Type? ResolveComponentType(Type? explicitType, string? title)
    {
        if (explicitType != null)
            return explicitType;

        if (string.IsNullOrWhiteSpace(title))
            return null;

        var firstWord = title.Trim().Split([' ', '<', '/'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstWord) &&
            s_tentoTypesByName.TryGetValue(firstWord, out var matched) &&
            typeof(IComponent).IsAssignableFrom(matched))
        {
            return matched;
        }

        return null;
    }

    public static IReadOnlyList<ApiTable.ApiProp> GetProperties(
        Type? componentType,
        IEnumerable<ApiTable.ApiProp>? explicitProperties = null,
        string? title = null)
    {
        var explicitList = explicitProperties?.ToList() ?? [];
        var resolvedType = ResolveComponentType(componentType, title);
        if (resolvedType == null)
        {
            return explicitList;
        }

        var explicitByProp = explicitList.ToDictionary(p => p.Property, p => p, StringComparer.OrdinalIgnoreCase);
        var classSince = resolvedType.GetCustomAttribute<SinceAttribute>()?.Version;

        object? defaultInstance = null;
        if (!resolvedType.IsAbstract && !resolvedType.ContainsGenericParameters)
        {
            try
            {
                defaultInstance = Activator.CreateInstance(resolvedType);
            }
            catch
            {
                // Ignore if no parameterless constructor
            }
        }

        var reflected = new List<ApiTable.ApiProp>();
        var props = resolvedType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null &&
                        !string.Equals(p.Name, "AdditionalAttributes", StringComparison.Ordinal))
            .OrderBy(p => p.DeclaringType == resolvedType ? 0 : 1)
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

    public static IReadOnlyList<ApiTable.ApiTypeInfo> GetReferencedTypes(
        Type? componentType,
        IReadOnlyList<ApiTable.ApiProp> properties,
        string? title = null)
    {
        var resolvedComponentType = ResolveComponentType(componentType, title);
        var discoveredTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        if (resolvedComponentType != null)
        {
            var props = resolvedComponentType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null);

            foreach (var prop in props)
            {
                CollectCustomTypes(prop.PropertyType, discoveredTypes);
            }
        }

        foreach (var prop in properties)
        {
            var rawType = prop.Type.TrimEnd('?');
            foreach (var token in rawType.Split(['<', '>', ',', ' ', '[', ']'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (s_tentoTypesByName.TryGetValue(token, out var t))
                {
                    CollectCustomTypes(t, discoveredTypes);
                }
            }
        }

        var result = new List<ApiTable.ApiTypeInfo>();
        foreach (var (_, type) in discoveredTypes.OrderBy(kv => kv.Value.IsEnum ? 0 : 1).ThenBy(kv => kv.Key))
        {
            if (type.IsEnum)
            {
                var names = Enum.GetNames(type);
                result.Add(new ApiTable.ApiTypeInfo(
                    type.Name,
                    "Enum",
                    names.Select(n => $"{type.Name}.{n}").ToArray(),
                    GetEnumDescription(type.Name)));
            }
            else if (type.Namespace?.StartsWith("TentoUI", StringComparison.Ordinal) == true &&
                     !typeof(IComponent).IsAssignableFrom(type) &&
                     !type.IsAbstract)
            {
                var pubProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => $"{p.Name}: {FormatTypeName(p.PropertyType)}")
                    .ToArray();
                if (pubProps.Length > 0)
                {
                    result.Add(new ApiTable.ApiTypeInfo(
                        FormatTypeName(type),
                        "Model",
                        pubProps,
                        $"Structured data model for {FormatTypeName(type)}."));
                }
            }
        }

        return result;
    }

    private static void CollectCustomTypes(Type type, Dictionary<string, Type> bucket)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying.IsArray && underlying.GetElementType() is { } elemType)
        {
            CollectCustomTypes(elemType, bucket);
            return;
        }

        if (underlying.IsGenericType)
        {
            foreach (var arg in underlying.GetGenericArguments())
            {
                CollectCustomTypes(arg, bucket);
            }
            return;
        }

        if (underlying.Namespace?.StartsWith("TentoUI", StringComparison.Ordinal) == true &&
            !typeof(IComponent).IsAssignableFrom(underlying) &&
            underlying != typeof(object))
        {
            bucket[underlying.Name] = underlying;
        }
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
        if (type == typeof(float)) return "float";
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
        "IconPosition" => "Placement of the icon relative to the button label (Start or End).",
        "IconName" => "Lucide icon identifier rendered inside the component.",
        "SpinSpeed" => "Controls the rotation speed when Spin is enabled (Slow, Normal, Fast).",
        "SpinDuration" => "Custom CSS animation duration for spinning icons (e.g. '1.5s', '400ms').",
        _ => $"Configures the {name} ({typeName}) parameter."
    };

    private static string GetEnumDescription(string enumName) => enumName switch
    {
        "ButtonType" => "Visual style variant of the button.",
        "ButtonShape" => "Border radius geometry of the button.",
        "ButtonHtmlType" => "Native HTML button type attribute.",
        "IconPosition" => "Side of the button where the icon is placed.",
        "ComponentSize" => "Standardized size scale across TentoUI components.",
        "ComponentStatus" => "Validation status visual state.",
        "FloatButtonType" => "Color variant of the floating action button.",
        "FloatButtonShape" => "Geometry of the floating action button.",
        "DividerType" => "Direction of the divider line.",
        "DividerOrientation" => "Horizontal alignment of the divider title text.",
        "IconSpinSpeed" => "Predefined rotation speed presets for spinning icons.",
        _ => $"Allowed values for {enumName}."
    };
}
