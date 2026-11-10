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
        s_componentMap["NumberInput"] = typeof(NumberInput<double>);
        s_componentMap["Segmented"] = typeof(Segmented<string>);
        s_componentMap["SegmentedItem"] = typeof(SegmentedItem<string>);
        s_componentMap["Radio"] = typeof(Radio<string>);
        s_componentMap["RadioGroup"] = typeof(RadioGroup<string>);
        s_componentMap["CheckboxGroup"] = typeof(CheckboxGroup<string>);
        s_componentMap["Select"] = typeof(Select<string, string>);
        s_componentMap["List"] = typeof(ListView<string>);
        s_componentMap["ListView"] = typeof(ListView<string>);
        s_componentMap["Table"] = typeof(Table<object>);
        s_componentMap["TableColumn"] = typeof(TableColumn<object>);
        s_componentMap["DataGridVirtual"] = typeof(DataGridVirtual<object>);
        s_componentMap["KanbanBoard"] = typeof(KanbanBoard<KanbanCardItem>);
        s_componentMap["StepperForm"] = typeof(StepperForm<object>);

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

            // 1. Check if there is an interactive Drawer, Modal, CommandPalette, or Tour that lacks an opening trigger button
            var hasOverlay = nodes.Any(n => n.TagName.Equals("Drawer", StringComparison.OrdinalIgnoreCase) ||
                                           n.TagName.Equals("Modal", StringComparison.OrdinalIgnoreCase) ||
                                           n.TagName.Equals("CommandPalette", StringComparison.OrdinalIgnoreCase) ||
                                           n.TagName.Equals("Tour", StringComparison.OrdinalIgnoreCase));
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

        // Handle IconNames.X -> resolve to canonical icon name
        if (val.StartsWith("@IconNames.", StringComparison.OrdinalIgnoreCase))
            val = LucideIcons.ResolveIconName(val[11..]);
        else if (val.StartsWith("IconNames.", StringComparison.OrdinalIgnoreCase))
            val = LucideIcons.ResolveIconName(val[10..]);

        return val;
    }

    private static bool IsVoidElement(string tag) =>
        !string.IsNullOrEmpty(tag) &&
        !char.IsUpper(tag[0]) &&
        (tag.Equals("br", StringComparison.OrdinalIgnoreCase) ||
         tag.Equals("hr", StringComparison.OrdinalIgnoreCase) ||
         tag.Equals("img", StringComparison.OrdinalIgnoreCase) ||
         tag.Equals("input", StringComparison.OrdinalIgnoreCase));

    private static bool IsInlineTag(string tag) =>
        tag.Equals("Button", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("FloatButton", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("BackTop", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Tag", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Badge", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Avatar", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Switch", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Rate", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("ColorPicker", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Icon", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Text", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Link", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("CopyButton", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Sparkline", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Kbd", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Gauge", StringComparison.OrdinalIgnoreCase);

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
                    else if (targetType == typeof(DateTime))
                    {
                        var cleanDate = rawVal.Trim().Trim('"', '\'', '@', '(', ')');
                        if (string.Equals(cleanDate, "DateTime.Today", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(cleanDate, "Today", StringComparison.OrdinalIgnoreCase))
                        {
                            builder.AddAttribute(seq++, prop.Name, DateTime.Today);
                        }
                        else if (string.Equals(cleanDate, "DateTime.Now", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(cleanDate, "Now", StringComparison.OrdinalIgnoreCase))
                        {
                            builder.AddAttribute(seq++, prop.Name, DateTime.Now);
                        }
                        else if (DateTime.TryParse(cleanDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtVal) ||
                                 DateTime.TryParse(cleanDate, out dtVal))
                        {
                            builder.AddAttribute(seq++, prop.Name, dtVal);
                        }
                    }
                    else if (targetType == typeof(DateOnly))
                    {
                        var cleanDate = rawVal.Trim().Trim('"', '\'', '@', '(', ')');
                        if (string.Equals(cleanDate, "DateTime.Today", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(cleanDate, "Today", StringComparison.OrdinalIgnoreCase) ||
                            cleanDate.Contains("FromDateTime", StringComparison.OrdinalIgnoreCase))
                        {
                            builder.AddAttribute(seq++, prop.Name, DateOnly.FromDateTime(DateTime.Today));
                        }
                        else if (DateOnly.TryParse(cleanDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var doVal) ||
                                 DateOnly.TryParse(cleanDate, out doVal))
                        {
                            builder.AddAttribute(seq++, prop.Name, doVal);
                        }
                    }
                    else if (targetType == typeof(TimeOnly))
                    {
                        var cleanTime = rawVal.Trim().Trim('"', '\'', '@', '(', ')');
                        if (string.Equals(cleanTime, "DateTime.Now", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(cleanTime, "Now", StringComparison.OrdinalIgnoreCase) ||
                            cleanTime.Contains("FromDateTime", StringComparison.OrdinalIgnoreCase))
                        {
                            builder.AddAttribute(seq++, prop.Name, TimeOnly.FromDateTime(DateTime.Now));
                        }
                        else if (TimeOnly.TryParse(cleanTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var toVal) ||
                                 TimeOnly.TryParse(cleanTime, out toVal))
                        {
                            builder.AddAttribute(seq++, prop.Name, toVal);
                        }
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
        // 2b. Tour open state and interactive dismiss
        else if (compType == typeof(Tour))
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
                builder.AddAttribute(seq++, "Name", LucideIcons.ResolveIconName(nameVal));
            }
        }
        // 10. Table / DataGridVirtual dummy rows for typed data visualization
        else if (compType == typeof(Table<object>) || compType == typeof(DataGridVirtual<object>))
        {
            if (!node.Attributes.ContainsKey("DataSource") || node.Attributes["DataSource"].StartsWith("@"))
            {
                var rowCount = compType == typeof(DataGridVirtual<object>) ? 1000 : 10;
                var dummyData = Enumerable.Range(1, rowCount)
                    .Select(i => (object)new
                    {
                        Id = 1000 + i,
                        Name = $"Engineer #{i:D3}",
                        Department = (i % 3 == 0) ? "Product" : (i % 2 == 0 ? "Design" : "Engineering"),
                        Salary = 90000m + (i * 150m),
                        Active = i % 5 != 0
                    }).ToList();
                builder.AddAttribute(seq++, "DataSource", (IEnumerable<object>)dummyData);
            }
        }
        // 10b. CodeBlock default code snippet
        else if (compType == typeof(CodeBlock))
        {
            if (!node.Attributes.ContainsKey("Code") || node.Attributes["Code"].StartsWith("@"))
            {
                var sampleCode = "public record UserDto(Guid Id, string Name, string Role);\n\npublic async Task<UserDto> GetUserAsync(Guid id)\n{\n    return await _repo.FindByIdAsync(id);\n}";
                builder.AddAttribute(seq++, "Code", sampleCode);
            }
        }
        // 10c. DiffViewer default old/new text
        else if (compType == typeof(DiffViewer))
        {
            if (!node.Attributes.ContainsKey("OldText") || node.Attributes["OldText"].StartsWith("@"))
            {
                builder.AddAttribute(seq++, "OldText", "public class OrderService\n{\n    public decimal TaxRate => 0.19m;\n}");
            }
            if (!node.Attributes.ContainsKey("NewText") || node.Attributes["NewText"].StartsWith("@"))
            {
                builder.AddAttribute(seq++, "NewText", "public class OrderService\n{\n    public decimal TaxRate => 0.21m;\n    public bool IsEuVat => true;\n}");
            }
        }
        // 10d. Transfer sample data fallback
        else if (compType == typeof(Transfer))
        {
            if (!node.Attributes.ContainsKey("DataSource") || node.Attributes["DataSource"].StartsWith("@"))
            {
                var sampleItems = new TransferItem[]
                {
                    new("admin", "Administrator", "Full system access"),
                    new("editor", "Content Editor", "Can publish blog posts"),
                    new("moderator", "Community Moderator", "Can flag user content"),
                    new("billing", "Billing Officer", "Manages invoices"),
                    new("developer", "Developer", "Access to API tokens and webhooks")
                };
                builder.AddAttribute(seq++, "DataSource", (IEnumerable<TransferItem>)sampleItems);
            }
        }
        // 10e. TreeSelect sample data fallback
        else if (compType == typeof(TreeSelect))
        {
            if (!node.Attributes.ContainsKey("TreeData") || node.Attributes["TreeData"].StartsWith("@"))
            {
                var sampleTree = new TreeDataNode[]
                {
                    new("src", "src",
                    [
                        new("components", "Components",
                        [
                            new("table", "Table.razor", isLeaf: true),
                            new("button", "Button.razor", isLeaf: true),
                            new("input", "Input.razor", isLeaf: true)
                        ]),
                        new("project", "TentoUI.csproj", isLeaf: true)
                    ]),
                    new("readme", "README.md", isLeaf: true)
                };
                builder.AddAttribute(seq++, "TreeData", (IEnumerable<TreeDataNode>)sampleTree);
            }
        }
        // 10f. Cascader sample data fallback
        else if (compType == typeof(Cascader))
        {
            if (!node.Attributes.ContainsKey("Options") || node.Attributes["Options"].StartsWith("@"))
            {
                var sampleCascader = new CascaderOption[]
                {
                    new("us", "United States",
                    [
                        new("ca", "California",
                        [
                            new("sf", "San Francisco"),
                            new("la", "Los Angeles")
                        ]),
                        new("ny", "New York",
                        [
                            new("nyc", "New York City"),
                            new("buf", "Buffalo")
                        ])
                    ]),
                    new("eu", "Europe",
                    [
                        new("de", "Germany",
                        [
                            new("ber", "Berlin"),
                            new("mun", "Munich")
                        ]),
                        new("pl", "Poland",
                        [
                            new("waw", "Warsaw"),
                            new("krk", "Krakow")
                        ])
                    ])
                };
                builder.AddAttribute(seq++, "Options", (IEnumerable<CascaderOption>)sampleCascader);
            }
        }
        // 11. CommandPalette open state and sample commands
        else if (compType == typeof(CommandPalette))
        {
            var isExplicitTrue = node.Attributes.TryGetValue("Open", out var cpVal) &&
                                 (string.Equals(cpVal, "true", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(cpVal, "1", StringComparison.OrdinalIgnoreCase));

            builder.AddAttribute(seq++, "Open", isExplicitTrue || _overlayOpen);
            builder.AddAttribute(seq++, "OpenChanged", EventCallback.Factory.Create<bool>(this, val =>
            {
                _overlayOpen = val;
                StateHasChanged();
            }));

            if (!node.Attributes.ContainsKey("Items") || node.Attributes["Items"].StartsWith("@"))
            {
                var sampleCommands = new[]
                {
                    new CommandItem("new-project", "Create New Project", "Workspace", "Initialize a new .NET 10 solution", IconNames.Plus, "Ctrl+N"),
                    new CommandItem("search-docs", "Search Documentation", "Navigation", "Jump to any component reference", IconNames.Search, "Ctrl+K"),
                    new CommandItem("theme-toggle", "Toggle Dark Mode", "Appearance", "Switch between light and dark themes", IconNames.Moon, "Ctrl+J")
                };
                builder.AddAttribute(seq++, "Items", (IEnumerable<CommandItem>)sampleCommands);
            }
        }
        // 12. ContextMenu default items
        else if (compType == typeof(ContextMenu))
        {
            if (!node.Attributes.ContainsKey("Items") || node.Attributes["Items"].StartsWith("@"))
            {
                var sampleMenu = new[]
                {
                    new DropdownMenuItem("copy", "Copy Selection", IconNames.Copy),
                    new DropdownMenuItem("edit", "Rename Item", IconNames.Edit2),
                    new DropdownMenuItem("delete", "Delete Permanently", IconNames.Trash2, Danger: true, DividerBefore: true)
                };
                builder.AddAttribute(seq++, "Items", (IEnumerable<DropdownMenuItem>)sampleMenu);
            }
        }
        // 12b. Dropdown default items when Overlay is not provided
        else if (compType == typeof(Dropdown))
        {
            var hasOverlayChild = node.Children.Any(c => c.TagName.Equals("Overlay", StringComparison.OrdinalIgnoreCase));
            if (!hasOverlayChild && (!node.Attributes.ContainsKey("Items") || node.Attributes["Items"].StartsWith("@")))
            {
                var sampleDropdown = new[]
                {
                    new DropdownMenuItem("profile", "Profile Settings", IconNames.User, Shortcut: "Ctrl+P"),
                    new DropdownMenuItem("billing", "Workspace Settings", IconNames.Settings),
                    new DropdownMenuItem("logout", "Delete Account", IconNames.Trash2, Danger: true, DividerBefore: true)
                };
                builder.AddAttribute(seq++, "Items", (IEnumerable<DropdownMenuItem>)sampleDropdown);
            }
        }
        // 13. KanbanBoard sample cards
        else if (compType == typeof(KanbanBoard<KanbanCardItem>))
        {
            if (!node.Attributes.ContainsKey("Items") || node.Attributes["Items"].StartsWith("@"))
            {
                var sampleCards = new System.Collections.Generic.List<KanbanCardItem>
                {
                    new() { Id = "k1", Title = "Design token audit", Description = "Verify --tento-* CSS custom properties", ColumnKey = "todo", Tag = "Design" },
                    new() { Id = "k2", Title = "Implement CommandPalette", Description = "Keyboard Ctrl+K spotlight search", ColumnKey = "in-progress", Tag = "Feature" },
                    new() { Id = "k3", Title = "WCAG 2.1 AA verification", Description = "All bUnit accessibility contracts green", ColumnKey = "done", Tag = "QA" }
                };
                builder.AddAttribute(seq++, "Items", (IEnumerable<KanbanCardItem>)sampleCards);
            }
        }
        // 14. Breadcrumb sample items fallback
        else if (compType == typeof(Breadcrumb))
        {
            if (!node.Children.Any(c => c.TagName.Equals("BreadcrumbItem", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode { TagName = "BreadcrumbItem", Attributes = new() { ["Href"] = "#" }, TextContent = "Home" });
                node.Children.Add(new MarkupNode { TagName = "BreadcrumbItem", Attributes = new() { ["Href"] = "#" }, TextContent = "Components" });
                node.Children.Add(new MarkupNode { TagName = "BreadcrumbItem", Attributes = new() { ["IsLast"] = "true" }, TextContent = "Breadcrumb" });
            }
        }
        // 15. Timeline sample items fallback
        else if (compType == typeof(Timeline))
        {
            if (!node.Children.Any(c => c.TagName.Equals("TimelineItem", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode { TagName = "TimelineItem", Attributes = new() { ["Color"] = "green", ["Label"] = "2026-10-28" }, TextContent = "Released TentoUI v1.0.0 on NuGet" });
                node.Children.Add(new MarkupNode { TagName = "TimelineItem", Attributes = new() { ["Color"] = "blue", ["Label"] = "2026-10-20" }, TextContent = "Completed 40+ AntDesign Tailwind components" });
                node.Children.Add(new MarkupNode { TagName = "TimelineItem", Attributes = new() { ["Color"] = "gray", ["Label"] = "2026-10-01" }, TextContent = "Extracted standalone git repository" });
            }
        }
        // 16. Steps sample steps fallback
        else if (compType == typeof(Steps))
        {
            if (!node.Children.Any(c => c.TagName.Equals("Step", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode { TagName = "Step", Attributes = new() { ["Title"] = "Account", ["Description"] = "Account setup" } });
                node.Children.Add(new MarkupNode { TagName = "Step", Attributes = new() { ["Title"] = "Verification", ["Description"] = "Identity check" } });
                node.Children.Add(new MarkupNode { TagName = "Step", Attributes = new() { ["Title"] = "Approval", ["Description"] = "Final confirmation" } });
            }
        }
        // 17. Collapse sample panels fallback
        else if (compType == typeof(Collapse))
        {
            if (!node.Children.Any(c => c.TagName.Equals("CollapsePanel", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode { TagName = "CollapsePanel", Attributes = new() { ["Key"] = "1", ["Header"] = "Infrastructure Topology" }, TextContent = "Multi-region AWS EKS clusters connected via Cilium service mesh." });
                node.Children.Add(new MarkupNode { TagName = "CollapsePanel", Attributes = new() { ["Key"] = "2", ["Header"] = "Database Failover" }, TextContent = "Automated PostgreSQL replication across availability zones." });
            }
        }
        // 18. Descriptions sample items fallback
        else if (compType == typeof(Descriptions))
        {
            if (!node.Children.Any(c => c.TagName.Equals("DescriptionsItem", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode { TagName = "DescriptionsItem", Attributes = new() { ["Label"] = "Product" }, TextContent = "Cloud Infrastructure Suite" });
                node.Children.Add(new MarkupNode { TagName = "DescriptionsItem", Attributes = new() { ["Label"] = "Billing Cycle" }, TextContent = "Monthly auto-renewal" });
                node.Children.Add(new MarkupNode { TagName = "DescriptionsItem", Attributes = new() { ["Label"] = "Status" }, TextContent = "Active" });
            }
        }
        // 19. ListView sample items fallback
        else if (compType == typeof(ListView<string>))
        {
            if (!node.Attributes.ContainsKey("DataSource") && node.Children.Count == 0 && string.IsNullOrEmpty(node.TextContent))
            {
                var sampleList = new[]
                {
                    "Kubernetes Cluster Deployment v1.31",
                    "Redis Enterprise Cache Layer",
                    "PostgreSQL Multi-AZ Primary Node",
                    "Prometheus Telemetry Exporter"
                };
                builder.AddAttribute(seq++, "DataSource", (IEnumerable<string>)sampleList);
            }
        }
        // 20. Tabs sample panes fallback
        else if (compType == typeof(Tabs))
        {
            if (!node.Children.Any(c => c.TagName.Equals("TabPane", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode { TagName = "TabPane", Attributes = new() { ["Key"] = "1", ["Tab"] = "Overview" }, TextContent = "System overview and health metrics." });
                node.Children.Add(new MarkupNode { TagName = "TabPane", Attributes = new() { ["Key"] = "2", ["Tab"] = "Deployments" }, TextContent = "Active rollouts across production clusters." });
                node.Children.Add(new MarkupNode { TagName = "TabPane", Attributes = new() { ["Key"] = "3", ["Tab"] = "Settings" }, TextContent = "Organization policies and security guardrails." });
            }
        }
        // 21. DateRangePicker OnRangeChange notification
        else if (compType == typeof(DateRangePicker))
        {
            if (!node.Attributes.ContainsKey("OnRangeChange"))
            {
                builder.AddAttribute(seq++, "OnRangeChange", EventCallback.Factory.Create<DateRange>(this, r =>
                {
                    MessageService?.Info($"Selected range: {r.Start:yyyy-MM-dd} → {r.End:yyyy-MM-dd}");
                    StateHasChanged();
                }));
            }
        }
        // 22. TimeRangePicker OnRangeChange notification
        else if (compType == typeof(TimeRangePicker))
        {
            if (!node.Attributes.ContainsKey("OnRangeChange"))
            {
                builder.AddAttribute(seq++, "OnRangeChange", EventCallback.Factory.Create<TimeRange>(this, r =>
                {
                    MessageService?.Info($"Selected time range: {r.Start:HH:mm} → {r.End:HH:mm}");
                    StateHasChanged();
                }));
            }
        }
        // 23. Calendar OnSelect notification
        else if (compType == typeof(Calendar))
        {
            if (!node.Attributes.ContainsKey("OnSelect"))
            {
                builder.AddAttribute(seq++, "OnSelect", EventCallback.Factory.Create<DateOnly>(this, d =>
                {
                    MessageService?.Info($"Selected date: {d:yyyy-MM-dd}");
                    StateHasChanged();
                }));
            }
        }
        // 24. Affix sample content fallback
        else if (compType == typeof(Affix))
        {
            if (node.Children.Count == 0 && string.IsNullOrEmpty(node.TextContent))
            {
                node.Children.Add(new MarkupNode { TagName = "div", Attributes = new() { ["Class"] = "px-4 py-2.5 rounded-xl bg-white/95 dark:bg-slate-900/95 backdrop-blur border border-primary-200 dark:border-primary-800 shadow-md text-xs font-semibold text-primary-700 dark:text-primary-300" }, TextContent = "Pinned Affix Toolbar" });
            }
        }
        // 25. Anchor sample links fallback
        else if (compType == typeof(Anchor))
        {
            if (!node.Children.Any(c => c.TagName.Equals("AnchorLink", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode { TagName = "AnchorLink", Attributes = new() { ["Href"] = "#basic", ["Title"] = "Basic Usage" } });
                node.Children.Add(new MarkupNode { TagName = "AnchorLink", Attributes = new() { ["Href"] = "#api", ["Title"] = "API Reference" } });
                node.Children.Add(new MarkupNode { TagName = "AnchorLink", Attributes = new() { ["Href"] = "#examples", ["Title"] = "Examples" } });
            }
        }
        // 26. Menu sample items fallback
        else if (compType == typeof(Menu))
        {
            if (!node.Children.Any(c => c.TagName.Equals("MenuItem", StringComparison.OrdinalIgnoreCase) ||
                                       c.TagName.Equals("SubMenu", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode { TagName = "MenuItem", Attributes = new() { ["Key"] = "dashboard", ["Title"] = "Dashboard" } });
                node.Children.Add(new MarkupNode { TagName = "MenuItem", Attributes = new() { ["Key"] = "analytics", ["Title"] = "Analytics" } });
                node.Children.Add(new MarkupNode { TagName = "MenuItem", Attributes = new() { ["Key"] = "settings", ["Title"] = "Settings" } });
            }
        }
        // 27. Space sample buttons fallback
        else if (compType == typeof(Space))
        {
            if (node.Children.Count == 0 && string.IsNullOrEmpty(node.TextContent))
            {
                node.Children.Add(new MarkupNode { TagName = "Button", Attributes = new() { ["Type"] = "ButtonType.Primary" }, TextContent = "Button 1" });
                node.Children.Add(new MarkupNode { TagName = "Button", Attributes = new() { ["Type"] = "ButtonType.Default" }, TextContent = "Button 2" });
                node.Children.Add(new MarkupNode { TagName = "Button", Attributes = new() { ["Type"] = "ButtonType.Dashed" }, TextContent = "Button 3" });
            }
        }
        // 28. Text sample content fallback
        else if (compType == typeof(Text))
        {
            if (node.Children.Count == 0 && string.IsNullOrEmpty(node.TextContent))
            {
                node.TextContent = "The quick brown fox jumps over the lazy dog";
            }
        }
        // 29. Transfer sample items fallback
        else if (compType == typeof(Transfer))
        {
            if (!node.Attributes.ContainsKey("DataSource") || node.Attributes["DataSource"].StartsWith("@"))
            {
                var sampleTransfer = new[]
                {
                    new TransferItem("1", "User Management", "Manage accounts and permissions"),
                    new TransferItem("2", "Billing & Invoices", "Payment gateways and receipts"),
                    new TransferItem("3", "Audit Logs", "Compliance and security trail"),
                    new TransferItem("4", "API Keys", "Developer tokens and webhooks"),
                    new TransferItem("5", "Notifications", "Email and Slack alerts")
                };
                builder.AddAttribute(seq++, "DataSource", (IEnumerable<TransferItem>)sampleTransfer);
            }
        }
        // 30. Tree sample nodes fallback
        else if (compType == typeof(Tree))
        {
            if ((!node.Attributes.ContainsKey("Data") || node.Attributes["Data"].StartsWith("@")) &&
                !node.Children.Any(c => c.TagName.Equals("TreeNode", StringComparison.OrdinalIgnoreCase)))
            {
                var sampleTree = new System.Collections.Generic.List<TreeDataNode>
                {
                    new("src", "src", [
                        new("src/components", "Components", [
                            new("src/components/button", "Button.razor", isLeaf: true),
                            new("src/components/modal", "Modal.razor", isLeaf: true),
                            new("src/components/table", "Table.razor", isLeaf: true)
                        ]),
                        new("src/services", "Services", [
                            new("src/services/api", "ApiService.cs", isLeaf: true)
                        ])
                    ]),
                    new("tests", "tests", [
                        new("tests/unit", "UnitTests.cs", isLeaf: true)
                    ])
                };
                builder.AddAttribute(seq++, "Data", (IEnumerable<TreeDataNode>)sampleTree);
                builder.AddAttribute(seq++, "ExpandedKeys", new HashSet<string> { "src", "src/components" });
            }
            if (!node.Attributes.ContainsKey("OnSelect"))
            {
                builder.AddAttribute(seq++, "OnSelect", EventCallback.Factory.Create<string>(this, key =>
                {
                    MessageService?.Info($"Selected tree node: {key}");
                    StateHasChanged();
                }));
            }
        }
        // 31. OTPInput OnComplete notification
        else if (compType == typeof(OTPInput))
        {
            if (!node.Attributes.ContainsKey("OnComplete"))
            {
                builder.AddAttribute(seq++, "OnComplete", EventCallback.Factory.Create<string>(this, code =>
                {
                    MessageService?.Success($"OTP code completed: {code}");
                    StateHasChanged();
                }));
            }
        }
        // 32. Mentions OnSelect notification
        else if (compType == typeof(Mentions))
        {
            if (!node.Attributes.ContainsKey("OnSelect"))
            {
                builder.AddAttribute(seq++, "OnSelect", EventCallback.Factory.Create<MentionOption>(this, opt =>
                {
                    MessageService?.Info($"Mention selected: {opt.EffectiveLabel}");
                    StateHasChanged();
                }));
            }
        }
        // 33. Image sample fallback
        else if (compType == typeof(Image))
        {
            if (!node.Attributes.ContainsKey("Src"))
            {
                builder.AddAttribute(seq++, "Src", "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?w=400&q=80");
            }
        }
        // 34. Carousel sample panels fallback
        else if (compType == typeof(Carousel))
        {
            if (!node.Children.Any(c => c.TagName.Equals("CarouselPanel", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode
                {
                    TagName = "CarouselPanel",
                    Children = [new MarkupNode { TagName = "div", Attributes = new() { ["Class"] = "h-48 bg-primary-600 text-white flex items-center justify-center font-bold text-xl rounded-lg" }, TextContent = "Slide 1: High Performance" }]
                });
                node.Children.Add(new MarkupNode
                {
                    TagName = "CarouselPanel",
                    Children = [new MarkupNode { TagName = "div", Attributes = new() { ["Class"] = "h-48 bg-emerald-600 text-white flex items-center justify-center font-bold text-xl rounded-lg" }, TextContent = "Slide 2: AntDesign Design Language" }]
                });
                node.Children.Add(new MarkupNode
                {
                    TagName = "CarouselPanel",
                    Children = [new MarkupNode { TagName = "div", Attributes = new() { ["Class"] = "h-48 bg-purple-600 text-white flex items-center justify-center font-bold text-xl rounded-lg" }, TextContent = "Slide 3: Tailwind CSS v4" }]
                });
            }
        }
        // 35. AudioPlayer sample fallback
        else if (compType == typeof(AudioPlayer))
        {
            if (!node.Attributes.ContainsKey("Title"))
            {
                builder.AddAttribute(seq++, "Title", "Ambient Flow");
            }
            if (!node.Attributes.ContainsKey("Artist"))
            {
                builder.AddAttribute(seq++, "Artist", "TentoUI Studio");
            }
        }
        // 36. CodeBlock sample fallback
        else if (compType == typeof(CodeBlock))
        {
            if (!node.Attributes.ContainsKey("Code"))
            {
                builder.AddAttribute(seq++, "Code", "public record User(string Name, string Role);\nvar admin = new User(\"Dominik\", \"Administrator\");\nConsole.WriteLine($\"Welcome, {admin.Name}!\");");
            }
        }
        // 37. Watermark sample content fallback
        else if (compType == typeof(Watermark))
        {
            if (node.Children.Count == 0 && string.IsNullOrEmpty(node.TextContent))
            {
                node.Children.Add(new MarkupNode
                {
                    TagName = "div",
                    Attributes = new() { ["Class"] = "h-48 p-6 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl space-y-2" },
                    Children = [
                        new MarkupNode { TagName = "div", Attributes = new() { ["Class"] = "font-bold text-slate-800 dark:text-white" }, TextContent = "Protected Document" },
                        new MarkupNode { TagName = "p", Attributes = new() { ["Class"] = "text-xs text-slate-500" }, TextContent = "This container is protected by an interactive SVG watermark background layer." }
                    ]
                });
            }
        }
        // 38. Splitter sample panels fallback
        else if (compType == typeof(Splitter))
        {
            if (!node.Children.Any(c => c.TagName.Equals("SplitterPanel", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode
                {
                    TagName = "SplitterPanel",
                    Children = [new MarkupNode { TagName = "div", Attributes = new() { ["Class"] = "p-4 text-xs font-semibold text-slate-700 dark:text-slate-300 bg-slate-50 dark:bg-slate-800/50 rounded-l-lg h-32 flex items-center justify-center" }, TextContent = "Left Panel" }]
                });
                node.Children.Add(new MarkupNode
                {
                    TagName = "SplitterPanel",
                    Children = [new MarkupNode { TagName = "div", Attributes = new() { ["Class"] = "p-4 text-xs font-semibold text-slate-700 dark:text-slate-300 bg-slate-100 dark:bg-slate-800 rounded-r-lg h-32 flex items-center justify-center" }, TextContent = "Right Panel" }]
                });
            }
        }
        // 39. Sparkline sample values fallback
        else if (compType == typeof(Sparkline))
        {
            if (!node.Attributes.ContainsKey("Values") && !node.Attributes.ContainsKey("Data"))
            {
                builder.AddAttribute(seq++, "Values", "12, 18, 14, 25, 22, 32, 28, 42");
            }
        }
        // 40. Layout sample structure fallback
        else if (compType == typeof(TentoUI.Layout))
        {
            if (!node.Children.Any(c => c.TagName.Equals("Header", StringComparison.OrdinalIgnoreCase) ||
                                       c.TagName.Equals("Sider", StringComparison.OrdinalIgnoreCase) ||
                                       c.TagName.Equals("Content", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode { TagName = "Header", Attributes = new() { ["Class"] = "bg-primary-600 text-white p-3 text-center text-xs font-bold" }, TextContent = "Header" });
                node.Children.Add(new MarkupNode { TagName = "Content", Attributes = new() { ["Class"] = "p-6 bg-slate-50 dark:bg-slate-900 text-center text-xs text-slate-600 dark:text-slate-300 min-h-[6rem]" }, TextContent = "Content Body" });
                node.Children.Add(new MarkupNode { TagName = "Footer", Attributes = new() { ["Class"] = "bg-slate-100 dark:bg-slate-800 p-2 text-center text-xs text-slate-500" }, TextContent = "Footer © TentoUI" });
            }
        }
        // 41. Row sample columns fallback
        else if (compType == typeof(Row))
        {
            if (!node.Children.Any(c => c.TagName.Equals("Col", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode
                {
                    TagName = "Col",
                    Attributes = new() { ["Span"] = "8" },
                    Children = [new MarkupNode { TagName = "div", Attributes = new() { ["Class"] = "bg-primary-500 text-white py-2 text-center rounded text-xs font-semibold" }, TextContent = "col-8" }]
                });
                node.Children.Add(new MarkupNode
                {
                    TagName = "Col",
                    Attributes = new() { ["Span"] = "8" },
                    Children = [new MarkupNode { TagName = "div", Attributes = new() { ["Class"] = "bg-primary-600 text-white py-2 text-center rounded text-xs font-semibold" }, TextContent = "col-8" }]
                });
                node.Children.Add(new MarkupNode
                {
                    TagName = "Col",
                    Attributes = new() { ["Span"] = "8" },
                    Children = [new MarkupNode { TagName = "div", Attributes = new() { ["Class"] = "bg-primary-700 text-white py-2 text-center rounded text-xs font-semibold" }, TextContent = "col-8" }]
                });
            }
        }
        // 42. Upload sample fallback
        else if (compType == typeof(Upload))
        {
            if (!node.Attributes.ContainsKey("ButtonText"))
            {
                builder.AddAttribute(seq++, "ButtonText", "Select Files to Upload");
            }
        }
        // 43. AvatarUpload sample fallback
        else if (compType == typeof(AvatarUpload))
        {
            if (!node.Attributes.ContainsKey("Src"))
            {
                builder.AddAttribute(seq++, "Src", "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=240&auto=format&fit=crop&q=80");
            }
        }
        // 44. SignaturePad sample fallback
        else if (compType == typeof(SignaturePad))
        {
            if (!node.Attributes.ContainsKey("Placeholder"))
            {
                builder.AddAttribute(seq++, "Placeholder", "Sign here to approve contract");
            }
        }
        // 45. MarkdownEditor sample fallback
        else if (compType == typeof(MarkdownEditor))
        {
            if (!node.Attributes.ContainsKey("Value"))
            {
                builder.AddAttribute(seq++, "Value", "# Welcome to TentoUI\n\n- Real-time markdown preview\n- Formatting toolbar\n- Syntax-highlighted output\n\n> Powered by .NET 10");
            }
        }
        // 46. Voiceform sample fallback
        else if (compType == typeof(Voiceform))
        {
            if (!node.Attributes.ContainsKey("Src"))
            {
                builder.AddAttribute(seq++, "Src", "audio/speech-sample.wav");
            }
            if (!node.Attributes.ContainsKey("Duration"))
            {
                builder.AddAttribute(seq++, "Duration", 5.0);
            }
            if (!node.Attributes.ContainsKey("Author"))
            {
                builder.AddAttribute(seq++, "Author", "Dominik");
            }
        }
        // 47. Form sample fallback
        else if (compType == typeof(Form))
        {
            if (!node.Attributes.ContainsKey("Model"))
            {
                builder.AddAttribute(seq++, "Model", new object());
            }
            if (!node.Children.Any(c => c.TagName.Equals("FormItem", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode
                {
                    TagName = "FormItem",
                    Attributes = new() { ["Label"] = "Full Name", ["Required"] = "true" },
                    Children = [new MarkupNode { TagName = "Input", Attributes = new() { ["Placeholder"] = "Enter full name..." } }]
                });
                node.Children.Add(new MarkupNode
                {
                    TagName = "FormItem",
                    Attributes = new() { ["Label"] = "Email Address", ["Required"] = "true" },
                    Children = [new MarkupNode { TagName = "Input", Attributes = new() { ["Placeholder"] = "name@company.com" } }]
                });
                node.Children.Add(new MarkupNode
                {
                    TagName = "Button",
                    Attributes = new() { ["Type"] = "ButtonType.Primary" },
                    TextContent = "Submit Form"
                });
            }
        }
        // 47b. StepperForm sample fallback
        else if (compType == typeof(StepperForm<object>))
        {
            if (!node.Attributes.ContainsKey("Model"))
            {
                builder.AddAttribute(seq++, "Model", new object());
            }
            if (!node.Children.Any(c => c.TagName.Equals("StepperFormStep", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode
                {
                    TagName = "StepperFormStep",
                    Attributes = new() { ["Title"] = "Account", ["SubTitle"] = "Personal Info", ["Icon"] = IconNames.User },
                    Children = [new MarkupNode { TagName = "FormItem", Attributes = new() { ["Label"] = "Full Name" }, Children = [new MarkupNode { TagName = "Input", Attributes = new() { ["Placeholder"] = "Enter name..." } }] }]
                });
                node.Children.Add(new MarkupNode
                {
                    TagName = "StepperFormStep",
                    Attributes = new() { ["Title"] = "Company", ["SubTitle"] = "Business profile", ["Icon"] = IconNames.Building },
                    Children = [new MarkupNode { TagName = "FormItem", Attributes = new() { ["Label"] = "Company Name" }, Children = [new MarkupNode { TagName = "Input", Attributes = new() { ["Placeholder"] = "Organization..." } }] }]
                });
                node.Children.Add(new MarkupNode
                {
                    TagName = "StepperFormStep",
                    Attributes = new() { ["Title"] = "Confirmation", ["SubTitle"] = "Finish setup", ["Icon"] = IconNames.CheckCircle },
                    Children = [new MarkupNode { TagName = "Text", TextContent = "Setup complete! Ready to submit." }]
                });
            }
        }
        // 48. FormWizard sample fallback
        else if (compType == typeof(FormWizard))
        {
            if (!node.Children.Any(c => c.TagName.Equals("WizardStep", StringComparison.OrdinalIgnoreCase)))
            {
                node.Children.Add(new MarkupNode
                {
                    TagName = "WizardStep",
                    Attributes = new() { ["Title"] = "Account Setup", ["Description"] = "Organization details" },
                    Children = [new MarkupNode { TagName = "Input", Attributes = new() { ["Placeholder"] = "Enter workspace name..." } }]
                });
                node.Children.Add(new MarkupNode
                {
                    TagName = "WizardStep",
                    Attributes = new() { ["Title"] = "Confirmation", ["Description"] = "Review & Deploy" },
                    Children = [new MarkupNode { TagName = "Text", TextContent = "Ready to deploy configuration." }]
                });
            }
        }
        // 49. DiffViewer sample fallback
        else if (compType == typeof(DiffViewer))
        {
            if (!node.Attributes.ContainsKey("OldText"))
            {
                builder.AddAttribute(seq++, "OldText", "{\n  \"version\": \"1.0.0\",\n  \"caching\": false\n}");
            }
            if (!node.Attributes.ContainsKey("NewText"))
            {
                builder.AddAttribute(seq++, "NewText", "{\n  \"version\": \"1.1.0\",\n  \"caching\": true,\n  \"provider\": \"HybridCache\"\n}");
            }
        }
        // 50. ContextMenu sample fallback
        else if (compType == typeof(ContextMenu))
        {
            if (!node.Attributes.ContainsKey("Items"))
            {
                var menuItems = new[]
                {
                    new DropdownMenuItem("copy", "Copy Selection", IconNames.Copy, Shortcut: "Ctrl+C"),
                    new DropdownMenuItem("duplicate", "Duplicate", IconNames.Plus, Shortcut: "Ctrl+D"),
                    new DropdownMenuItem("delete", "Delete Item", IconNames.Trash2, Danger: true, DividerBefore: true)
                };
                builder.AddAttribute(seq++, "Items", (IEnumerable<DropdownMenuItem>)menuItems);
            }
            if (node.Children.Count == 0 && string.IsNullOrEmpty(node.TextContent))
            {
                node.Children.Add(new MarkupNode
                {
                    TagName = "div",
                    Attributes = new() { ["Class"] = "h-36 rounded-xl border-2 border-dashed border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-900/50 flex flex-col items-center justify-center gap-2 text-slate-500 cursor-context-menu" },
                    TextContent = "Right-click anywhere inside this container"
                });
            }
        }
        // 51. ImagePreviewGroup sample fallback
        else if (compType == typeof(ImagePreviewGroup))
        {
            if (node.Children.Count == 0 && !node.Attributes.ContainsKey("Images"))
            {
                node.Children.Add(new MarkupNode { TagName = "Image", Attributes = new() { ["Src"] = "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=400&q=80", ["Width"] = "140px", ["Height"] = "90px" } });
                node.Children.Add(new MarkupNode { TagName = "Image", Attributes = new() { ["Src"] = "https://images.unsplash.com/photo-1469474968028-56623f02e42e?w=400&q=80", ["Width"] = "140px", ["Height"] = "90px" } });
                node.Children.Add(new MarkupNode { TagName = "Image", Attributes = new() { ["Src"] = "https://images.unsplash.com/photo-1501785888041-af3ef285b470?w=400&q=80", ["Width"] = "140px", ["Height"] = "90px" } });
            }
        }
        // 52. ChatBubble sample fallback
        else if (compType == typeof(ChatBubble))
        {
            if (!node.Attributes.ContainsKey("Message"))
            {
                builder.AddAttribute(seq++, "Message", "Hello! How can I help you build with TentoUI today?");
            }
            if (!node.Attributes.ContainsKey("Author"))
            {
                builder.AddAttribute(seq++, "Author", "Tento Copilot");
            }
        }
        // 53. PromptInput sample fallback
        else if (compType == typeof(PromptInput))
        {
            if (!node.Attributes.ContainsKey("Placeholder"))
            {
                builder.AddAttribute(seq++, "Placeholder", "Ask anything about TentoUI...");
            }
            if (!node.Attributes.ContainsKey("SuggestionsString"))
            {
                builder.AddAttribute(seq++, "SuggestionsString", "Generate form, Table virtualization, Theme tokens");
            }
        }
        // 54. CommandPalette sample fallback
        else if (compType == typeof(CommandPalette))
        {
            if (!node.Attributes.ContainsKey("Items"))
            {
                var cmds = new[]
                {
                    new CommandItem("new", "Create Project", "Workspace", "Initialize new solution", IconNames.Plus, "Ctrl+N"),
                    new CommandItem("docs", "Search Docs", "Help", "Browse component APIs", IconNames.Search, "Ctrl+K"),
                    new CommandItem("theme", "Toggle Theme", "Appearance", "Switch dark/light", IconNames.Moon, "Ctrl+J")
                };
                builder.AddAttribute(seq++, "Items", (IEnumerable<CommandItem>)cmds);
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
