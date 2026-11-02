using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using TentoUI;

namespace TentoUI.Docs.Components;

/// <summary>
/// Universal dynamic markup renderer for TentoUI interactive live demos.
/// Automatically discovers and binds the entire TentoUI component suite via a high-performance
/// reflection component registry, stack-based AST parser, and attribute normalizer.
/// Supports live editing of any TentoUI component without hardcoded switch-case limitations.
/// </summary>
public class TentoMarkupRenderer : ComponentBase
{
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    private IMessageService? MessageService => ServiceProvider.GetService(typeof(IMessageService)) as IMessageService;
    private INotificationService? NotificationService => ServiceProvider.GetService(typeof(INotificationService)) as INotificationService;
    private IModalService? ModalService => ServiceProvider.GetService(typeof(IModalService)) as IModalService;

    [Parameter] public string? Markup { get; set; }
    [Parameter] public RenderFragment? Fallback { get; set; }

    public string? ParseError { get; private set; }

    private bool _overlayOpen;

    private static readonly Dictionary<string, Type> s_componentMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> s_parameterCache = new();

    static TentoMarkupRenderer()
    {
        // 1. Scan and register all non-generic component types from TentoUI assembly
        var tentoTypes = typeof(Button).Assembly.GetTypes()
            .Where(t => typeof(IComponent).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var type in tentoTypes)
        {
            if (!type.IsGenericTypeDefinition)
            {
                s_componentMap[type.Name] = type;
            }
        }

        // 2. Register closed generic components with standard/sensible default type arguments
        s_componentMap["InputNumber"] = typeof(InputNumber<double>);
        s_componentMap["Segmented"] = typeof(Segmented<string>);
        s_componentMap["SegmentedItem"] = typeof(SegmentedItem<string>);
        s_componentMap["Radio"] = typeof(Radio<string>);
        s_componentMap["RadioGroup"] = typeof(RadioGroup<string>);
        s_componentMap["CheckboxGroup"] = typeof(CheckboxGroup<string>);
        s_componentMap["Select"] = typeof(Select<string, string>);
        s_componentMap["List"] = typeof(List<string>);
        s_componentMap["Table"] = typeof(Table<object>);
        s_componentMap["TableColumn"] = typeof(TableColumn<object>);

        // 3. Precache parameter properties for fast runtime resolution
        foreach (var (_, type) in s_componentMap)
        {
            CacheParametersForType(type);
        }
    }

    private static Dictionary<string, PropertyInfo> GetParameterProperties(Type type)
    {
        if (!s_parameterCache.TryGetValue(type, out var dict))
        {
            dict = CacheParametersForType(type);
        }
        return dict;
    }

    private static Dictionary<string, PropertyInfo> CacheParametersForType(Type type)
    {
        var dict = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
        s_parameterCache[type] = dict;
        return dict;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(Markup))
        {
            Fallback?.Invoke(builder);
            return;
        }

        try
        {
            ParseError = null;
            var nodes = ParseNodes(Markup);
            if (nodes.Count == 0)
            {
                Fallback?.Invoke(builder);
                return;
            }

            var seq = 0;

            // 1. Check if there is an interactive Drawer or Modal that lacks an opening trigger button
            var hasOverlay = nodes.Any(n => n.TagName.Equals("Drawer", StringComparison.OrdinalIgnoreCase) ||
                                           n.TagName.Equals("Modal", StringComparison.OrdinalIgnoreCase));
            var hasButtonTrigger = nodes.Any(n => n.TagName.Equals("Button", StringComparison.OrdinalIgnoreCase));

            if (hasOverlay && !hasButtonTrigger && !_overlayOpen)
            {
                builder.OpenComponent<Button>(seq++);
                builder.AddAttribute(seq++, "Type", ButtonType.Primary);
                builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, () =>
                {
                    _overlayOpen = true;
                    StateHasChanged();
                }));
                builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Open Panel")));
                builder.CloseComponent();
            }

            // 2. Standalone Popover, Tooltip, or Dropdown root: provide generous spacing and centering so floating overlays never clip
            var isFloatingRoot = nodes.Count == 1 &&
                (nodes[0].TagName.Equals("Popover", StringComparison.OrdinalIgnoreCase) ||
                 nodes[0].TagName.Equals("Tooltip", StringComparison.OrdinalIgnoreCase) ||
                 nodes[0].TagName.Equals("Dropdown", StringComparison.OrdinalIgnoreCase));

            if (isFloatingRoot)
            {
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "py-14 flex items-center justify-center w-full min-h-[140px] relative overflow-visible");
                RenderNode(nodes[0], builder, ref seq);
                builder.CloseElement();
                return;
            }

            // 3. If all top-level elements are inline widgets (e.g. multiple Buttons, Tags, Badges), wrap in a flex row
            var needsFlexWrap = nodes.Count > 1 && nodes.All(n => IsInlineTag(n.TagName));
            if (needsFlexWrap)
            {
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "flex flex-wrap items-center gap-3 w-full");
                foreach (var node in nodes)
                {
                    RenderNode(node, builder, ref seq);
                }
                builder.CloseElement();
            }
            else
            {
                foreach (var node in nodes)
                {
                    RenderNode(node, builder, ref seq);
                }
            }
        }
        catch (Exception ex)
        {
            ParseError = ex.Message;
            Fallback?.Invoke(builder);
        }
    }

    public class MarkupNode
    {
        public string TagName { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty;
        public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public System.Collections.Generic.List<MarkupNode> Children { get; set; } = new();
    }

    /// <summary>
    /// Parses markup into a hierarchical AST using a stack-based algorithm.
    /// Handles arbitrarily nested tags of identical names (divs, carousels, cards, panels),
    /// lambda expressions in attributes (@(() => ...)), and nested quotes.
    /// </summary>
    public static System.Collections.Generic.List<MarkupNode> ParseNodes(string input)
    {
        var roots = new System.Collections.Generic.List<MarkupNode>();
        if (string.IsNullOrWhiteSpace(input)) return roots;

        var stack = new Stack<MarkupNode>();
        var pos = 0;
        var len = input.Length;

        while (pos < len)
        {
            var nextLt = input.IndexOf('<', pos);
            if (nextLt == -1)
            {
                var text = input[pos..].Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    var textNode = new MarkupNode { TextContent = text };
                    AddNode(textNode, stack, roots);
                }
                break;
            }

            if (nextLt > pos)
            {
                var text = input[pos..nextLt].Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    var textNode = new MarkupNode { TextContent = text };
                    AddNode(textNode, stack, roots);
                }
            }

            pos = nextLt;

            // HTML comments: <!-- ... -->
            if (pos + 3 < len && input.Substring(pos, 4) == "<!--")
            {
                var commentEnd = input.IndexOf("-->", pos + 4, StringComparison.Ordinal);
                pos = (commentEnd == -1) ? len : commentEnd + 3;
                continue;
            }

            // Razor comments: @* ... *@
            if (pos + 1 < len && input.Substring(pos, 2) == "@*")
            {
                var commentEnd = input.IndexOf("*@", pos + 2, StringComparison.Ordinal);
                pos = (commentEnd == -1) ? len : commentEnd + 2;
                continue;
            }

            // Find closing tag bracket '>' respecting quotes, parentheses, and lambdas
            var nextGt = FindTagClosingBracket(input, pos + 1);
            if (nextGt == -1) break;

            var rawTagContent = input.Substring(pos + 1, nextGt - pos - 1).Trim();
            pos = nextGt + 1;

            if (string.IsNullOrEmpty(rawTagContent)) continue;

            // Closing tag: </TagName>
            if (rawTagContent.StartsWith('/'))
            {
                var closingTagName = rawTagContent[1..].Trim();
                if (closingTagName.StartsWith("TentoUI.", StringComparison.OrdinalIgnoreCase))
                    closingTagName = closingTagName[8..];

                // Pop matching tag from stack
                if (stack.Count > 0)
                {
                    var found = false;
                    foreach (var item in stack)
                    {
                        if (string.Equals(item.TagName, closingTagName, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        while (stack.Count > 0)
                        {
                            var top = stack.Pop();
                            if (string.Equals(top.TagName, closingTagName, StringComparison.OrdinalIgnoreCase))
                                break;
                        }
                    }
                }
                continue;
            }

            // Opening tag or self-closing tag
            var isSelfClosing = rawTagContent.EndsWith('/');
            if (isSelfClosing)
                rawTagContent = rawTagContent[..^1].Trim();

            var (tagName, attributes) = ParseTagHeader(rawTagContent);
            if (string.IsNullOrEmpty(tagName)) continue;

            var node = new MarkupNode
            {
                TagName = tagName,
                Attributes = attributes
            };

            AddNode(node, stack, roots);

            if (!isSelfClosing && !IsVoidElement(tagName))
            {
                stack.Push(node);
            }
        }

        return roots;
    }

    private static int FindTagClosingBracket(string input, int startPos)
    {
        var len = input.Length;
        var inQuote = '\0';
        var parenDepth = 0;

        for (var i = startPos; i < len; i++)
        {
            var c = input[i];

            if (inQuote != '\0')
            {
                if (c == '(') parenDepth++;
                else if (c == ')') { if (parenDepth > 0) parenDepth--; }
                else if (c == inQuote && parenDepth == 0)
                {
                    inQuote = '\0';
                }
            }
            else
            {
                if (c == '"' || c == '\'')
                {
                    inQuote = c;
                }
                else if (c == '>')
                {
                    return i;
                }
            }
        }
        return -1;
    }

    private static void AddNode(MarkupNode node, Stack<MarkupNode> stack, System.Collections.Generic.List<MarkupNode> roots)
    {
        if (stack.Count > 0)
            stack.Peek().Children.Add(node);
        else
            roots.Add(node);
    }

    public static (string TagName, Dictionary<string, string> Attributes) ParseTagHeader(string header)
    {
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(header)) return (string.Empty, attrs);

        var i = 0;
        var len = header.Length;

        // 1. Tag name
        while (i < len && char.IsWhiteSpace(header[i])) i++;
        var tagStart = i;
        while (i < len && !char.IsWhiteSpace(header[i]) && header[i] != '/' && header[i] != '>') i++;
        if (tagStart == i) return (string.Empty, attrs);

        var tagName = header[tagStart..i];
        if (tagName.StartsWith("TentoUI.", StringComparison.OrdinalIgnoreCase))
            tagName = tagName[8..];

        // 2. Attributes
        while (i < len)
        {
            // Skip whitespace
            while (i < len && char.IsWhiteSpace(header[i])) i++;
            if (i >= len) break;

            if (header[i] == '/' || header[i] == '>')
            {
                i++;
                continue;
            }

            // Attribute name
            var keyStart = i;
            while (i < len && !char.IsWhiteSpace(header[i]) && header[i] != '=' && header[i] != '/' && header[i] != '>')
                i++;
            var rawKey = header[keyStart..i].Trim();
            if (string.IsNullOrEmpty(rawKey)) break;

            // Skip whitespace after key
            while (i < len && char.IsWhiteSpace(header[i])) i++;

            string rawVal;
            if (i < len && header[i] == '=')
            {
                i++; // consume '='
                while (i < len && char.IsWhiteSpace(header[i])) i++;

                if (i < len && (header[i] == '"' || header[i] == '\''))
                {
                    var quoteChar = header[i];
                    i++; // consume opening quote
                    var valStart = i;
                    var parenDepth = 0;

                    while (i < len)
                    {
                        var c = header[i];
                        if (c == '(') parenDepth++;
                        else if (c == ')') { if (parenDepth > 0) parenDepth--; }
                        else if (c == quoteChar && parenDepth == 0)
                        {
                            break;
                        }
                        i++;
                    }

                    rawVal = header[valStart..i];
                    if (i < len && header[i] == quoteChar) i++; // consume closing quote
                }
                else
                {
                    // Bare unquoted value (e.g. key=val)
                    var valStart = i;
                    while (i < len && !char.IsWhiteSpace(header[i]) && header[i] != '/' && header[i] != '>')
                        i++;
                    rawVal = header[valStart..i];
                }
            }
            else
            {
                // Bare flag attribute (e.g. Disabled, Danger, Striped)
                rawVal = "true";
            }

            // Normalize key: strip @bind- or leading @
            var key = rawKey;
            if (key.StartsWith("@bind-", StringComparison.OrdinalIgnoreCase))
                key = key[6..];
            else if (key.StartsWith("@bind:", StringComparison.OrdinalIgnoreCase))
                key = key[6..];
            else if (key.StartsWith("@", StringComparison.OrdinalIgnoreCase))
                key = key[1..];

            // Normalize value: strip leading wrapping quotes
            var val = NormalizeAttributeValue(rawVal);
            attrs[key] = val;
        }

        return (tagName, attrs);
    }

    private static string NormalizeAttributeValue(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        val = val.Trim();

        // Handle @("...") or @"..."
        if (val.StartsWith("@(\"") && val.EndsWith("\")"))
            return val[3..^2];
        if (val.StartsWith("@\"") && val.EndsWith("\""))
            return val[2..^1];

        // Handle IconNames.X -> x
        if (val.StartsWith("IconNames.", StringComparison.OrdinalIgnoreCase))
            val = val[10..].ToLowerInvariant();
        else if (val.StartsWith("@IconNames.", StringComparison.OrdinalIgnoreCase))
            val = val[11..].ToLowerInvariant();

        return val;
    }

    private static bool IsVoidElement(string tag) =>
        tag.Equals("br", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("hr", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("img", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("input", StringComparison.OrdinalIgnoreCase);

    private static bool IsInlineTag(string tag) =>
        tag.Equals("Button", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Tag", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Badge", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Avatar", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Switch", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Rate", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("ColorPicker", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Icon", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Text", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Link", StringComparison.OrdinalIgnoreCase);

    private void RenderNode(MarkupNode node, RenderTreeBuilder builder, ref int seq)
    {
        // 1. Plain text node
        if (string.IsNullOrEmpty(node.TagName))
        {
            if (!string.IsNullOrEmpty(node.TextContent))
            {
                builder.AddContent(seq++, node.TextContent);
            }
            return;
        }

        var tag = node.TagName;

        // 2. Check if tag matches a TentoUI component
        if (s_componentMap.TryGetValue(tag, out var compType))
        {
            RenderDynamicComponent(compType, node, builder, ref seq);
            return;
        }

        // 3. Otherwise render as HTML element (div, span, p, h1, etc.)
        builder.OpenElement(seq++, tag.ToLowerInvariant());
        foreach (var (k, v) in node.Attributes)
        {
            builder.AddAttribute(seq++, k.ToLowerInvariant(), v);
        }

        if (!string.IsNullOrEmpty(node.TextContent))
        {
            builder.AddContent(seq++, node.TextContent);
        }

        foreach (var child in node.Children)
        {
            RenderNode(child, builder, ref seq);
        }

        builder.CloseElement();
    }

    private void RenderDynamicComponent(Type compType, MarkupNode node, RenderTreeBuilder builder, ref int seq)
    {
        builder.OpenComponent(seq++, compType);

        var paramProps = GetParameterProperties(compType);

        // Identify child elements that represent named RenderFragment parameters (like <Prefix>, <Suffix>, <Icon>, etc.)
        var namedFragments = new HashSet<MarkupNode>();
        foreach (var child in node.Children)
        {
            if (!string.IsNullOrEmpty(child.TagName) &&
                paramProps.TryGetValue(child.TagName, out var rfProp) &&
                rfProp.PropertyType == typeof(RenderFragment))
            {
                namedFragments.Add(child);
                var targetChild = child;
                builder.AddAttribute(seq++, rfProp.Name, (RenderFragment)(rfBuilder =>
                {
                    var rfSeq = 0;
                    foreach (var inner in targetChild.Children)
                    {
                        RenderNode(inner, rfBuilder, ref rfSeq);
                    }
                }));
            }
        }

        // Bind attributes
        foreach (var (attrKey, rawVal) in node.Attributes)
        {
            // Skip Razor type arguments (e.g. TValue, TItem)
            if (attrKey.Equals("TValue", StringComparison.OrdinalIgnoreCase) ||
                attrKey.Equals("TItem", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (paramProps.TryGetValue(attrKey, out var prop))
            {
                // Handle EventCallback parameters (like OnClick)
                if (prop.PropertyType == typeof(EventCallback<MouseEventArgs>))
                {
                    var cb = CreateMouseCallback(rawVal);
                    if (cb.HasDelegate)
                    {
                        builder.AddAttribute(seq++, prop.Name, cb);
                        continue;
                    }
                }
                else if (prop.PropertyType == typeof(EventCallback))
                {
                    var cb = CreateSimpleCallback(rawVal);
                    if (cb.HasDelegate)
                    {
                        builder.AddAttribute(seq++, prop.Name, cb);
                        continue;
                    }
                }

                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                try
                {
                    if (targetType == typeof(string))
                    {
                        builder.AddAttribute(seq++, prop.Name, rawVal);
                    }
                    else if (targetType == typeof(bool))
                    {
                        var bVal = string.Equals(rawVal, "true", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(rawVal, "@true", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(rawVal, "1", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(rawVal, attrKey, StringComparison.OrdinalIgnoreCase) ||
                                   string.IsNullOrEmpty(rawVal);
                        builder.AddAttribute(seq++, prop.Name, bVal);
                    }
                    else if (targetType == typeof(int))
                    {
                        if (int.TryParse(rawVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var iVal))
                            builder.AddAttribute(seq++, prop.Name, iVal);
                        else
                            builder.AddAttribute(seq++, prop.Name, 0);
                    }
                    else if (targetType == typeof(double))
                    {
                        if (double.TryParse(rawVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var dVal))
                            builder.AddAttribute(seq++, prop.Name, dVal);
                    }
                    else if (targetType == typeof(decimal))
                    {
                        if (decimal.TryParse(rawVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var mVal))
                            builder.AddAttribute(seq++, prop.Name, mVal);
                    }
                    else if (targetType.IsEnum)
                    {
                        var cleanEnum = NormalizeEnumString(rawVal);
                        if (Enum.TryParse(targetType, cleanEnum, ignoreCase: true, out var enumVal))
                        {
                            builder.AddAttribute(seq++, prop.Name, enumVal);
                        }
                    }
                    else if (targetType == typeof(RenderFragment))
                    {
                        builder.AddAttribute(seq++, prop.Name, (RenderFragment)(rf => rf.AddContent(0, rawVal)));
                    }
                    else if (targetType == typeof(object))
                    {
                        builder.AddAttribute(seq++, prop.Name, (object)rawVal);
                    }
                }
                catch
                {
                    // Ignore conversion exceptions
                }
            }
        }

        // Apply special sensible defaults for generic or dynamic components
        ApplySpecialComponentAttributes(compType, node, builder, ref seq);

        // Bind ChildContent
        var contentChildren = node.Children.Where(c => !namedFragments.Contains(c)).ToList();
        if (paramProps.ContainsKey("ChildContent"))
        {
            if (contentChildren.Count > 0)
            {
                builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    var childSeq = 0;
                    foreach (var child in contentChildren)
                    {
                        RenderNode(child, childBuilder, ref childSeq);
                    }
                }));
            }
            else if (!string.IsNullOrEmpty(node.TextContent))
            {
                builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    childBuilder.AddContent(0, node.TextContent);
                }));
            }
        }

        builder.CloseComponent();
    }

    private void ApplySpecialComponentAttributes(Type compType, MarkupNode node, RenderTreeBuilder builder, ref int seq)
    {
        // 1. Drawer open state and interactive dismiss
        if (compType == typeof(Drawer))
        {
            var isExplicitTrue = node.Attributes.TryGetValue("Open", out var oVal) &&
                                 (string.Equals(oVal, "true", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(oVal, "1", StringComparison.OrdinalIgnoreCase));

            builder.AddAttribute(seq++, "Open", isExplicitTrue || _overlayOpen);

            builder.AddAttribute(seq++, "OnClose", EventCallback.Factory.Create(this, () =>
            {
                _overlayOpen = false;
                StateHasChanged();
            }));

            builder.AddAttribute(seq++, "OpenChanged", EventCallback.Factory.Create<bool>(this, val =>
            {
                _overlayOpen = val;
                StateHasChanged();
            }));
        }
        // 2. Modal visible state and interactive dismiss
        else if (compType == typeof(Modal))
        {
            var isExplicitTrue = node.Attributes.TryGetValue("Visible", out var vVal) &&
                                 (string.Equals(vVal, "true", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(vVal, "1", StringComparison.OrdinalIgnoreCase));

            builder.AddAttribute(seq++, "Visible", isExplicitTrue || _overlayOpen);

            builder.AddAttribute(seq++, "VisibleChanged", EventCallback.Factory.Create<bool>(this, val =>
            {
                _overlayOpen = val;
                StateHasChanged();
            }));

            builder.AddAttribute(seq++, "OnCancel", EventCallback.Factory.Create(this, () =>
            {
                _overlayOpen = false;
                StateHasChanged();
            }));

            builder.AddAttribute(seq++, "OnOk", EventCallback.Factory.Create(this, () =>
            {
                _overlayOpen = false;
                MessageService?.Success("Modal action confirmed!");
                StateHasChanged();
            }));
        }
        // 3. Segmented default options
        else if (compType == typeof(Segmented<string>))
        {
            var hasChildItems = node.Children.Any(c => c.TagName.Equals("SegmentedItem", StringComparison.OrdinalIgnoreCase));
            if (!hasChildItems)
            {
                if (node.Attributes.TryGetValue("Options", out var optVal) &&
                    !optVal.StartsWith("@") &&
                    !optVal.Equals("options", StringComparison.OrdinalIgnoreCase) &&
                    !optVal.Equals("iconOptions", StringComparison.OrdinalIgnoreCase) &&
                    !optVal.Equals("disabledOptions", StringComparison.OrdinalIgnoreCase))
                {
                    builder.AddAttribute(seq++, "Options", (object)optVal);
                }
                else
                {
                    builder.AddAttribute(seq++, "Options", (object)"Daily, Weekly, Monthly, Quarterly");
                }
            }
            if (!node.Attributes.ContainsKey("Value"))
            {
                builder.AddAttribute(seq++, "Value", "Daily");
            }
        }
        // 4. Select default options
        else if (compType == typeof(Select<string, string>))
        {
            if (!node.Attributes.ContainsKey("DataSource") || node.Attributes["DataSource"].StartsWith("@"))
            {
                var defaultItems = new[] { "Option 1", "Option 2", "Option 3" };
                builder.AddAttribute(seq++, "DataSource", (IEnumerable<string>)defaultItems);
            }
        }
        // 5. RadioGroup default selected value
        else if (compType == typeof(RadioGroup<string>))
        {
            if (!node.Attributes.ContainsKey("Value"))
            {
                var firstRadio = node.Children.FirstOrDefault(c => c.TagName.Equals("Radio", StringComparison.OrdinalIgnoreCase));
                if (firstRadio != null && firstRadio.Attributes.TryGetValue("Value", out var val))
                {
                    builder.AddAttribute(seq++, "Value", val);
                }
            }
        }
        // 6. Rate default score if bound to a variable
        else if (compType == typeof(Rate))
        {
            if (!node.Attributes.ContainsKey("Value") || !int.TryParse(node.Attributes["Value"], out _))
            {
                builder.AddAttribute(seq++, "Value", 4);
            }
        }
        // 7. Slider default value
        else if (compType == typeof(Slider))
        {
            if (!node.Attributes.ContainsKey("Value") || !double.TryParse(node.Attributes["Value"], out _))
            {
                builder.AddAttribute(seq++, "Value", 35.0);
            }
        }
        // 8. Switch default checked state if variable bound
        else if (compType == typeof(Switch))
        {
            if (!node.Attributes.ContainsKey("Checked"))
            {
                builder.AddAttribute(seq++, "Checked", true);
            }
        }
        // 9. Icon name normalization
        else if (compType == typeof(Icon))
        {
            if (node.Attributes.TryGetValue("Name", out var nameVal))
            {
                var cleanName = nameVal;
                if (cleanName.StartsWith("IconNames.", StringComparison.OrdinalIgnoreCase))
                    cleanName = cleanName[10..];
                if (cleanName.StartsWith("@IconNames.", StringComparison.OrdinalIgnoreCase))
                    cleanName = cleanName[11..];
                if (cleanName.StartsWith("@"))
                    cleanName = cleanName[1..];
                builder.AddAttribute(seq++, "Name", cleanName.ToLowerInvariant());
            }
        }
        // 10. Table dummy rows for typed data visualization
        else if (compType == typeof(Table<object>))
        {
            if (!node.Attributes.ContainsKey("DataSource") || node.Attributes["DataSource"].StartsWith("@"))
            {
                var dummyData = new System.Collections.Generic.List<object>
                {
                    new { Id = 101, Name = "Alice Johnson", Department = "Engineering", Salary = 135000m, Active = true },
                    new { Id = 102, Name = "Bob Smith", Department = "Product", Salary = 115000m, Active = true },
                    new { Id = 103, Name = "Charlie Davis", Department = "Engineering", Salary = 145000m, Active = false },
                    new { Id = 104, Name = "Diana Prince", Department = "Marketing", Salary = 95000m, Active = true }
                };
                builder.AddAttribute(seq++, "DataSource", (IEnumerable<object>)dummyData);
            }
        }
    }

    private EventCallback<MouseEventArgs> CreateMouseCallback(string rawVal)
    {
        var action = ResolveAction(rawVal);
        return EventCallback.Factory.Create<MouseEventArgs>(this, action);
    }

    private EventCallback CreateSimpleCallback(string rawVal)
    {
        var action = ResolveAction(rawVal);
        return EventCallback.Factory.Create(this, action);
    }

    private Action ResolveAction(string rawVal)
    {
        if (string.IsNullOrWhiteSpace(rawVal))
        {
            return () => { };
        }

        // 1. MessageService toast trigger
        var msgMatch = Regex.Match(rawVal, @"(?:MessageService|Message)\.(Success|Info|Warning|Error|Loading)\s*\(\s*""([^""]*)""(?:\s*,\s*([0-9\.]+))?", RegexOptions.IgnoreCase);
        if (msgMatch.Success)
        {
            var method = msgMatch.Groups[1].Value.ToLowerInvariant();
            var content = msgMatch.Groups[2].Value;
            double? duration = msgMatch.Groups[3].Success && double.TryParse(msgMatch.Groups[3].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;

            return () =>
            {
                if (MessageService == null) return;
                switch (method)
                {
                    case "success": MessageService.Success(content, duration ?? 3); break;
                    case "info": MessageService.Info(content, duration ?? 3); break;
                    case "warning": MessageService.Warning(content, duration ?? 3); break;
                    case "error": MessageService.Error(content, duration ?? 3); break;
                    case "loading": MessageService.Loading(content, duration ?? 0); break;
                }
            };
        }

        // 2. NotificationService corner notice trigger
        var notifMatch = Regex.Match(rawVal, @"(?:NotificationService|Notification)\.(Success|Info|Warning|Error)\s*\(\s*""([^""]*)""(?:\s*,\s*""([^""]*)"")?(?:\s*,\s*([0-9\.]+))?", RegexOptions.IgnoreCase);
        if (notifMatch.Success)
        {
            var method = notifMatch.Groups[1].Value.ToLowerInvariant();
            var title = notifMatch.Groups[2].Value;
            var desc = notifMatch.Groups[3].Success ? notifMatch.Groups[3].Value : null;
            double? duration = notifMatch.Groups[4].Success && double.TryParse(notifMatch.Groups[4].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;

            return () =>
            {
                if (NotificationService == null) return;
                switch (method)
                {
                    case "success": NotificationService.Success(title, desc, duration ?? 4.5); break;
                    case "info": NotificationService.Info(title, desc, duration ?? 4.5); break;
                    case "warning": NotificationService.Warning(title, desc, duration ?? 4.5); break;
                    case "error": NotificationService.Error(title, desc, duration ?? 4.5); break;
                }
            };
        }

        // 3. Open Drawer / Modal action
        if (rawVal.Contains("true", StringComparison.OrdinalIgnoreCase) ||
            rawVal.Contains("open", StringComparison.OrdinalIgnoreCase) ||
            rawVal.Contains("modal", StringComparison.OrdinalIgnoreCase) ||
            rawVal.Contains("drawer", StringComparison.OrdinalIgnoreCase) ||
            rawVal.Contains("show", StringComparison.OrdinalIgnoreCase) ||
            rawVal.Contains("edit", StringComparison.OrdinalIgnoreCase))
        {
            return () =>
            {
                _overlayOpen = true;
                StateHasChanged();
            };
        }

        // 4. Close Drawer / Modal action
        if (rawVal.Contains("false", StringComparison.OrdinalIgnoreCase) ||
            rawVal.Contains("close", StringComparison.OrdinalIgnoreCase) ||
            rawVal.Contains("hide", StringComparison.OrdinalIgnoreCase) ||
            rawVal.Contains("cancel", StringComparison.OrdinalIgnoreCase) ||
            rawVal.Contains("save", StringComparison.OrdinalIgnoreCase))
        {
            return () =>
            {
                _overlayOpen = false;
                StateHasChanged();
            };
        }

        return () => { };
    }

    private static string NormalizeEnumString(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        val = val.Trim();
        if (val.StartsWith('@')) val = val[1..].Trim();
        if (val.Contains('.')) val = val[(val.LastIndexOf('.') + 1)..];
        return val;
    }
}
