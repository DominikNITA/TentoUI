using System.Reflection;
using Microsoft.AspNetCore.Components;
using TentoUI.Docs.Components;
using TentoUI.Extensibility;

namespace TentoUI.Docs.Services;

/// <summary>
/// Reflects <see cref="ParameterAttribute"/>, <see cref="SinceAttribute"/>, <see cref="ObsoleteAttribute"/>,
/// public record/model properties, and referenced enum types directly from TentoUI types.
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
            !matched.IsEnum)
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
        var isComponent = typeof(IComponent).IsAssignableFrom(resolvedType);

        object? defaultInstance = null;
        if (!resolvedType.IsAbstract && !resolvedType.ContainsGenericParameters)
        {
            try
            {
                defaultInstance = Activator.CreateInstance(resolvedType);
            }
            catch
            {
                // Ignore if no parameterless constructor (e.g. positional records)
            }
        }

        // Map constructor parameter defaults for positional records/models (e.g. CommandItem, DropdownMenuItem)
        var ctorDefaults = new Dictionary<string, ParameterInfo>(StringComparer.OrdinalIgnoreCase);
        if (!isComponent)
        {
            var primaryCtor = resolvedType
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (primaryCtor != null)
            {
                foreach (var param in primaryCtor.GetParameters())
                {
                    if (!string.IsNullOrEmpty(param.Name))
                    {
                        ctorDefaults[param.Name] = param;
                    }
                }
            }
        }

        var reflected = new List<ApiTable.ApiProp>();
        var props = resolvedType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => (!isComponent || p.GetCustomAttribute<ParameterAttribute>() != null) &&
                        !string.Equals(p.Name, "AdditionalAttributes", StringComparison.Ordinal))
            .OrderBy(p => p.DeclaringType == resolvedType ? 0 : 1)
            .ThenBy(p => isComponent ? p.Name : string.Empty);

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
                var defaultVal = FormatDefaultValue(prop, defaultInstance, ctorDefaults);
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
            var isComponent = typeof(IComponent).IsAssignableFrom(resolvedComponentType);
            var props = resolvedComponentType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !isComponent || p.GetCustomAttribute<ParameterAttribute>() != null);

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
        foreach (var (_, type) in discoveredTypes.OrderBy(kv => kv.Key))
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

    private static string? FormatDefaultValue(
        PropertyInfo prop,
        object? instance,
        Dictionary<string, ParameterInfo> ctorDefaults)
    {
        if (instance != null)
        {
            try
            {
                var val = prop.GetValue(instance);
                return FormatRawDefaultValue(val);
            }
            catch { }
        }

        if (ctorDefaults.TryGetValue(prop.Name, out var ctorParam))
        {
            if (ctorParam.HasDefaultValue)
            {
                return FormatRawDefaultValue(ctorParam.DefaultValue);
            }
            return "(required)";
        }

        return null;
    }

    private static string? FormatRawDefaultValue(object? val)
    {
        if (val == null) return "null";
        if (val is bool b) return b ? "true" : "false";
        if (val is string s) return string.IsNullOrEmpty(s) ? "\"\"" : $"\"{s}\"";
        if (val.GetType().IsEnum) return $"{val.GetType().Name}.{val}";
        if (val is ValueType && !val.GetType().Name.StartsWith("EventCallback", StringComparison.Ordinal))
        {
            return Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture);
        }
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
        "Key" => "Unique identifier for the item passed to selection callbacks.",
        "Label" => "Display text rendered for the item.",
        "Group" => "Category group header under which the item is organized.",
        "Description" => "Secondary descriptive text displayed alongside the primary label.",
        "Icon" => "Optional Lucide icon name rendered before the item label.",
        "Shortcut" => "Keyboard shortcut hint badge displayed on the right side of the item.",
        "Danger" => "Applies destructive red styling to indicate a dangerous action.",
        "DividerBefore" => "Renders a horizontal separator line immediately above this item.",
        "Vertical" => "Stacks flex children vertically (flex-col) when true, or horizontally (flex-row) when false.",
        "Justify" => "Controls alignment of flex items along the main axis.",
        "Align" => "Controls alignment of flex items along the cross axis.",
        "Wrap" => "Controls whether flex items wrap onto multiple lines.",
        "Gap" => "Predefined spacing scale between flex children (None, Small, Middle, Large).",
        "GapSize" => "Custom CSS gap value (e.g. '12px', '1.5rem') that overrides the preset Gap.",
        "Gutter" => "Horizontal spacing between grid columns in pixels.",
        "VerticalGutter" => "Vertical spacing between wrapped grid rows in pixels.",
        "Span" => "Number of columns (1–24) the column spans across the row.",
        "Offset" => "Number of columns (0–23) to offset the column from the left.",
        "Order" => "Flex order index controlling visual column ordering.",
        "Xs" => "Column span on extra-small viewports (<576px).",
        "Sm" => "Column span on small viewports (≥576px).",
        "Md" => "Column span on medium viewports (≥768px).",
        "Lg" => "Column span on large viewports (≥992px).",
        "Xl" => "Column span on extra-large viewports (≥1200px).",
        "Xxl" => "Column span on 2XL viewports (≥1600px).",
        "OffsetTop" => "Distance in pixels from the top of the scrolling container before sticking.",
        "OffsetBottom" => "Distance in pixels from the bottom of the scrolling container before sticking.",
        "Affix" => "Wraps the anchor navigation in a sticky container when true.",
        "ShowInkInFixed" => "Displays the active indicator bar along the anchor rail.",
        "Href" => "Target URL or section hash selector (e.g. '#section-1').",
        "Title" => "Primary text title displayed for the element.",
        "Target" => "HTML anchor target attribute (e.g. '_blank').",
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
        "FlexJustify" => "Main-axis distribution of children inside Flex or Row.",
        "FlexAlign" => "Cross-axis alignment of children inside Flex or Row.",
        "FlexWrap" => "Line wrapping behavior of flex children.",
        "FlexGap" => "Preset gap spacing scale between flex children.",
        "RowJustify" => "Horizontal alignment of columns inside a 24-column Row.",
        "RowAlign" => "Vertical alignment of columns inside a 24-column Row.",
        "DropdownTrigger" => "User interaction that opens the dropdown menu.",
        "DropdownPlacement" => "Preferred placement of the dropdown overlay relative to the trigger.",
        "FilterLogicalOperator" => "Logical operator (And, Or) used to combine filter rules.",
        "FilterComparisonOperator" => "Comparison condition operator (Contains, Equals, StartsWith, EndsWith, GreaterThan, LessThan).",
        _ => $"Allowed values for {enumName}."
    };
}
