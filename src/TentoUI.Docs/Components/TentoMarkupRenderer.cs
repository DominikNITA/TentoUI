using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using TentoUI;

namespace TentoUI.Docs.Components;

/// <summary>
/// Dynamically parses and renders Razor/HTML snippets for TentoUI interactive live demos.
/// Employs a robust stack-based tree parser that correctly handles nested tags of identical names,
/// attribute normalization (stripping Razor '@' prefixes, unquoting values, handling bare boolean flags),
/// and comprehensive component mapping across the entire TentoUI component suite.
/// </summary>
public class TentoMarkupRenderer : ComponentBase
{
    [Parameter] public string? Markup { get; set; }
    [Parameter] public RenderFragment? Fallback { get; set; }

    public string? ParseError { get; private set; }

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
            foreach (var node in nodes)
            {
                RenderNode(node, builder, ref seq);
            }
        }
        catch (Exception ex)
        {
            ParseError = ex.Message;
            var seq = 0;
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "p-3 my-2 rounded-lg border border-amber-300 dark:border-amber-800 bg-amber-50 dark:bg-amber-950/40 text-amber-800 dark:text-amber-200 text-xs flex items-center gap-2");
            builder.AddContent(seq++, "⚠️ Syntax error while rendering preview: " + ex.Message);
            builder.CloseElement();

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
    /// Perfectly handles arbitrarily nested tags of identical names (like nested divs, spaces, buttons, or cards).
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

            // Find closing tag bracket '>'
            var nextGt = input.IndexOf('>', pos);
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
                    // Find if the tag is on stack
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

    private static void AddNode(MarkupNode node, Stack<MarkupNode> stack, System.Collections.Generic.List<MarkupNode> roots)
    {
        if (stack.Count > 0)
            stack.Peek().Children.Add(node);
        else
            roots.Add(node);
    }

    private static (string TagName, Dictionary<string, string> Attributes) ParseTagHeader(string header)
    {
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(header)) return (string.Empty, attrs);

        var match = Regex.Match(header, @"^([a-zA-Z0-9_\.:]+)");
        if (!match.Success) return (string.Empty, attrs);

        var tagName = match.Groups[1].Value;
        if (tagName.StartsWith("TentoUI.", StringComparison.OrdinalIgnoreCase))
            tagName = tagName[8..];

        var rest = header[match.Length..].Trim();

        // Match attributes: key="value", key='value', key=@value, or bare key
        var attrMatches = Regex.Matches(rest, @"([@a-zA-Z0-9_\-\.:]+)(?:\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s>]+)))?");

        foreach (Match m in attrMatches)
        {
            var rawKey = m.Groups[1].Value;
            var val = m.Groups[2].Success ? m.Groups[2].Value
                    : m.Groups[3].Success ? m.Groups[3].Value
                    : m.Groups[4].Success ? m.Groups[4].Value
                    : "true"; // bare attribute flag

            // Normalize key: strip @bind- or leading @
            var key = rawKey;
            if (key.StartsWith("@bind-", StringComparison.OrdinalIgnoreCase))
                key = key[6..];
            else if (key.StartsWith("@bind:", StringComparison.OrdinalIgnoreCase))
                key = key[6..];
            else if (key.StartsWith("@", StringComparison.OrdinalIgnoreCase))
                key = key[1..];

            // Normalize value: strip leading @ or wrapping @("...")
            val = NormalizeAttributeValue(val);

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

        // Handle leading @ (e.g. @ButtonType.Primary, @IconNames.Search, @true, @false)
        if (val.StartsWith('@'))
            val = val[1..].Trim();

        // Handle IconNames.X -> x
        if (val.StartsWith("IconNames.", StringComparison.OrdinalIgnoreCase))
            val = val[10..].ToLowerInvariant();

        return val;
    }

    private static bool IsVoidElement(string tag) =>
        tag.Equals("br", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("hr", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("img", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("input", StringComparison.OrdinalIgnoreCase);

    private static void RenderNode(MarkupNode node, RenderTreeBuilder builder, ref int seq)
    {
        if (string.IsNullOrEmpty(node.TagName))
        {
            builder.AddContent(seq++, node.TextContent);
            return;
        }

        var tag = node.TagName;

        // General
        if (tag.Equals("Button", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Button>(seq++);
            if (node.Attributes.TryGetValue("Type", out var typeVal))
                builder.AddAttribute(seq++, "Type", ParseButtonType(typeVal));
            if (node.Attributes.TryGetValue("Size", out var sizeVal))
                builder.AddAttribute(seq++, "Size", ParseSize(sizeVal));
            if (node.Attributes.TryGetValue("Shape", out var shapeVal))
                builder.AddAttribute(seq++, "Shape", ParseButtonShape(shapeVal));
            if (node.Attributes.TryGetValue("Danger", out var dangerVal))
                builder.AddAttribute(seq++, "Danger", ParseBool(dangerVal));
            if (node.Attributes.TryGetValue("Ghost", out var ghostVal))
                builder.AddAttribute(seq++, "Ghost", ParseBool(ghostVal));
            if (node.Attributes.TryGetValue("Loading", out var loadingVal))
                builder.AddAttribute(seq++, "Loading", ParseBool(loadingVal));
            if (node.Attributes.TryGetValue("Disabled", out var disabledVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disabledVal));
            if (node.Attributes.TryGetValue("Block", out var blockVal))
                builder.AddAttribute(seq++, "Block", ParseBool(blockVal));
            if (node.Attributes.TryGetValue("Href", out var hrefVal))
                builder.AddAttribute(seq++, "Href", hrefVal);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);

            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("FloatButton", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<FloatButton>(seq++);
            if (node.Attributes.TryGetValue("Type", out var typeVal))
                builder.AddAttribute(seq++, "Type", ParseButtonType(typeVal));
            if (node.Attributes.TryGetValue("Shape", out var shapeVal))
                builder.AddAttribute(seq++, "Shape", ParseButtonShape(shapeVal));
            if (node.Attributes.TryGetValue("Description", out var descVal))
                builder.AddAttribute(seq++, "Description", descVal);
            if (node.Attributes.TryGetValue("Tooltip", out var tipVal))
                builder.AddAttribute(seq++, "Tooltip", tipVal);
            if (node.Attributes.TryGetValue("Href", out var hrefVal))
                builder.AddAttribute(seq++, "Href", hrefVal);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else if (tag.Equals("BackTop", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<BackTop>(seq++);
            if (node.Attributes.TryGetValue("VisibilityHeight", out var vhVal) && int.TryParse(vhVal, out var vh))
                builder.AddAttribute(seq++, "VisibilityHeight", vh);
            builder.CloseComponent();
        }
        else if (tag.Equals("Icon", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Icon>(seq++);
            if (node.Attributes.TryGetValue("Name", out var nameVal))
                builder.AddAttribute(seq++, "Name", NormalizeAttributeValue(nameVal));
            if (node.Attributes.TryGetValue("Size", out var sizeVal) && int.TryParse(sizeVal, out var size))
                builder.AddAttribute(seq++, "Size", size);
            if (node.Attributes.TryGetValue("Spin", out var spinVal))
                builder.AddAttribute(seq++, "Spin", ParseBool(spinVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else if (tag.Equals("Title", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Title>(seq++);
            if (node.Attributes.TryGetValue("Level", out var lvlVal) && int.TryParse(lvlVal, out var lvl))
                builder.AddAttribute(seq++, "Level", lvl);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Text", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Text>(seq++);
            if (node.Attributes.TryGetValue("Type", out var typeVal))
                builder.AddAttribute(seq++, "Type", ParseTypographyType(typeVal));
            if (node.Attributes.TryGetValue("Strong", out var strongVal))
                builder.AddAttribute(seq++, "Strong", ParseBool(strongVal));
            if (node.Attributes.TryGetValue("Italic", out var itVal))
                builder.AddAttribute(seq++, "Italic", ParseBool(itVal));
            if (node.Attributes.TryGetValue("Code", out var codeVal))
                builder.AddAttribute(seq++, "Code", ParseBool(codeVal));
            if (node.Attributes.TryGetValue("Keyboard", out var kbdVal))
                builder.AddAttribute(seq++, "Keyboard", ParseBool(kbdVal));
            if (node.Attributes.TryGetValue("Delete", out var delVal))
                builder.AddAttribute(seq++, "Delete", ParseBool(delVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Paragraph", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Paragraph>(seq++);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Link", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Link>(seq++);
            if (node.Attributes.TryGetValue("Href", out var hrefVal))
                builder.AddAttribute(seq++, "Href", hrefVal);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Divider", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Divider>(seq++);
            if (node.Attributes.TryGetValue("Dashed", out var dashedVal))
                builder.AddAttribute(seq++, "Dashed", ParseBool(dashedVal));
            if (node.Attributes.TryGetValue("Vertical", out var vertVal))
                builder.AddAttribute(seq++, "Vertical", ParseBool(vertVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            if (node.Children.Count > 0)
            {
                builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    var cSeq = 0;
                    foreach (var child in node.Children)
                        RenderNode(child, childBuilder, ref cSeq);
                }));
            }
            builder.CloseComponent();
        }
        // Layout & Navigation
        else if (tag.Equals("Space", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Space>(seq++);
            if (node.Attributes.TryGetValue("Direction", out var dirVal))
                builder.AddAttribute(seq++, "Direction", ParseSpaceDirection(dirVal));
            if (node.Attributes.TryGetValue("Wrap", out var wrapVal))
                builder.AddAttribute(seq++, "Wrap", ParseBool(wrapVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Row", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Row>(seq++);
            if (node.Attributes.TryGetValue("Gutter", out var gVal) && int.TryParse(gVal, out var gutter))
                builder.AddAttribute(seq++, "Gutter", gutter);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Col", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Col>(seq++);
            if (node.Attributes.TryGetValue("Span", out var sVal) && int.TryParse(sVal, out var span))
                builder.AddAttribute(seq++, "Span", span);
            if (node.Attributes.TryGetValue("Offset", out var oVal) && int.TryParse(oVal, out var offset))
                builder.AddAttribute(seq++, "Offset", offset);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Breadcrumb", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Breadcrumb>(seq++);
            if (node.Attributes.TryGetValue("Separator", out var sepVal))
                builder.AddAttribute(seq++, "Separator", sepVal);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("BreadcrumbItem", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<BreadcrumbItem>(seq++);
            if (node.Attributes.TryGetValue("Href", out var hrefVal))
                builder.AddAttribute(seq++, "Href", hrefVal);
            if (node.Attributes.TryGetValue("IsLast", out var lastVal))
                builder.AddAttribute(seq++, "IsLast", ParseBool(lastVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Dropdown", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Dropdown>(seq++);
            if (node.Attributes.TryGetValue("Text", out var textVal))
                builder.AddAttribute(seq++, "Text", textVal);
            if (node.Attributes.TryGetValue("Disabled", out var disVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);

            var overlayNode = node.Children.FirstOrDefault(c => c.TagName.Equals("Overlay", StringComparison.OrdinalIgnoreCase));
            if (overlayNode != null)
            {
                builder.AddAttribute(seq++, "Overlay", (RenderFragment)(oBuilder =>
                {
                    var oSeq = 0;
                    foreach (var oc in overlayNode.Children)
                        RenderNode(oc, oBuilder, ref oSeq);
                }));
            }
            builder.CloseComponent();
        }
        else if (tag.Equals("Pagination", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Pagination>(seq++);
            if (node.Attributes.TryGetValue("Current", out var cVal) && int.TryParse(cVal, out var current))
                builder.AddAttribute(seq++, "Current", current);
            if (node.Attributes.TryGetValue("PageSize", out var psVal) && int.TryParse(psVal, out var pageSize))
                builder.AddAttribute(seq++, "PageSize", pageSize);
            if (node.Attributes.TryGetValue("Total", out var tVal) && int.TryParse(tVal, out var total))
                builder.AddAttribute(seq++, "Total", total);
            if (node.Attributes.TryGetValue("ShowSizeChanger", out var sscVal))
                builder.AddAttribute(seq++, "ShowSizeChanger", ParseBool(sscVal));
            if (node.Attributes.TryGetValue("ShowQuickJumper", out var sqjVal))
                builder.AddAttribute(seq++, "ShowQuickJumper", ParseBool(sqjVal));
            if (node.Attributes.TryGetValue("Simple", out var simVal))
                builder.AddAttribute(seq++, "Simple", ParseBool(simVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else if (tag.Equals("Steps", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Steps>(seq++);
            if (node.Attributes.TryGetValue("Current", out var cVal) && int.TryParse(cVal, out var cur))
                builder.AddAttribute(seq++, "Current", cur);
            if (node.Attributes.TryGetValue("Direction", out var dirVal))
                builder.AddAttribute(seq++, "Direction", ParseStepsDirection(dirVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Step", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Step>(seq++);
            if (node.Attributes.TryGetValue("Title", out var titleVal))
                builder.AddAttribute(seq++, "Title", titleVal);
            if (node.Attributes.TryGetValue("Description", out var descVal))
                builder.AddAttribute(seq++, "Description", descVal);
            if (node.Attributes.TryGetValue("Disabled", out var disVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disVal));
            builder.CloseComponent();
        }
        else if (tag.Equals("Tabs", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Tabs>(seq++);
            if (node.Attributes.TryGetValue("Type", out var typeVal))
                builder.AddAttribute(seq++, "Type", ParseTabType(typeVal));
            if (node.Attributes.TryGetValue("DefaultActiveKey", out var dakVal))
                builder.AddAttribute(seq++, "DefaultActiveKey", dakVal);
            if (node.Attributes.TryGetValue("ActiveKey", out var akVal))
                builder.AddAttribute(seq++, "ActiveKey", akVal);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("TabPane", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<TabPane>(seq++);
            if (node.Attributes.TryGetValue("Key", out var keyVal))
                builder.AddAttribute(seq++, "Key", keyVal);
            if (node.Attributes.TryGetValue("Tab", out var tabVal))
                builder.AddAttribute(seq++, "Tab", tabVal);
            if (node.Attributes.TryGetValue("Disabled", out var disVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disVal));
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        // Data Entry
        else if (tag.Equals("Input", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Input>(seq++);
            if (node.Attributes.TryGetValue("Placeholder", out var pVal))
                builder.AddAttribute(seq++, "Placeholder", pVal);
            if (node.Attributes.TryGetValue("Value", out var val))
                builder.AddAttribute(seq++, "Value", val);
            if (node.Attributes.TryGetValue("Disabled", out var disabledVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disabledVal));
            if (node.Attributes.TryGetValue("AllowClear", out var clearVal))
                builder.AddAttribute(seq++, "AllowClear", ParseBool(clearVal));
            if (node.Attributes.TryGetValue("Size", out var sizeVal))
                builder.AddAttribute(seq++, "Size", ParseSize(sizeVal));
            if (node.Attributes.TryGetValue("Type", out var typeVal))
                builder.AddAttribute(seq++, "Type", typeVal.Equals("password", StringComparison.OrdinalIgnoreCase) ? InputType.Password : InputType.Text);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);

            var prefixNode = node.Children.FirstOrDefault(c => c.TagName.Equals("Prefix", StringComparison.OrdinalIgnoreCase));
            if (prefixNode != null)
            {
                builder.AddAttribute(seq++, "Prefix", (RenderFragment)(pBuilder =>
                {
                    var pSeq = 0;
                    foreach (var pc in prefixNode.Children)
                        RenderNode(pc, pBuilder, ref pSeq);
                }));
            }
            var suffixNode = node.Children.FirstOrDefault(c => c.TagName.Equals("Suffix", StringComparison.OrdinalIgnoreCase));
            if (suffixNode != null)
            {
                builder.AddAttribute(seq++, "Suffix", (RenderFragment)(sBuilder =>
                {
                    var sSeq = 0;
                    foreach (var sc in suffixNode.Children)
                        RenderNode(sc, sBuilder, ref sSeq);
                }));
            }
            builder.CloseComponent();
        }
        else if (tag.Equals("InputNumber", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<InputNumber<double>>(seq++);
            if (node.Attributes.TryGetValue("Value", out var vVal) && double.TryParse(vVal, out var val))
                builder.AddAttribute(seq++, "Value", val);
            if (node.Attributes.TryGetValue("Min", out var minVal) && double.TryParse(minVal, out var min))
                builder.AddAttribute(seq++, "Min", min);
            if (node.Attributes.TryGetValue("Max", out var maxVal) && double.TryParse(maxVal, out var max))
                builder.AddAttribute(seq++, "Max", max);
            if (node.Attributes.TryGetValue("Step", out var stepVal) && double.TryParse(stepVal, out var step))
                builder.AddAttribute(seq++, "Step", step);
            if (node.Attributes.TryGetValue("Controls", out var cVal))
                builder.AddAttribute(seq++, "Controls", ParseBool(cVal));
            if (node.Attributes.TryGetValue("Disabled", out var dVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(dVal));
            if (node.Attributes.TryGetValue("Size", out var sVal))
                builder.AddAttribute(seq++, "Size", ParseSize(sVal));
            builder.CloseComponent();
        }
        else if (tag.Equals("TextArea", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<TextArea>(seq++);
            if (node.Attributes.TryGetValue("Placeholder", out var pVal))
                builder.AddAttribute(seq++, "Placeholder", pVal);
            if (node.Attributes.TryGetValue("Value", out var val))
                builder.AddAttribute(seq++, "Value", val);
            if (node.Attributes.TryGetValue("Rows", out var rowsVal) && int.TryParse(rowsVal, out var rows))
                builder.AddAttribute(seq++, "Rows", rows);
            if (node.Attributes.TryGetValue("Disabled", out var disabledVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disabledVal));
            if (node.Attributes.TryGetValue("AllowClear", out var clearVal))
                builder.AddAttribute(seq++, "AllowClear", ParseBool(clearVal));
            if (node.Attributes.TryGetValue("ShowCount", out var countVal))
                builder.AddAttribute(seq++, "ShowCount", ParseBool(countVal));
            if (node.Attributes.TryGetValue("MaxLength", out var mlVal) && int.TryParse(mlVal, out var ml))
                builder.AddAttribute(seq++, "MaxLength", ml);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else if (tag.Equals("Checkbox", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Checkbox>(seq++);
            if (node.Attributes.TryGetValue("Checked", out var cVal))
                builder.AddAttribute(seq++, "Checked", ParseBool(cVal));
            if (node.Attributes.TryGetValue("Indeterminate", out var indVal))
                builder.AddAttribute(seq++, "Indeterminate", ParseBool(indVal));
            if (node.Attributes.TryGetValue("Disabled", out var disVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Switch", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Switch>(seq++);
            if (node.Attributes.TryGetValue("Checked", out var checkedVal))
                builder.AddAttribute(seq++, "Checked", ParseBool(checkedVal));
            if (node.Attributes.TryGetValue("Disabled", out var disabledVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disabledVal));
            if (node.Attributes.TryGetValue("Loading", out var loadingVal))
                builder.AddAttribute(seq++, "Loading", ParseBool(loadingVal));
            if (node.Attributes.TryGetValue("Size", out var sizeVal))
                builder.AddAttribute(seq++, "Size", ParseSize(sizeVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else if (tag.Equals("Rate", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Rate>(seq++);
            if (node.Attributes.TryGetValue("Value", out var vVal) && int.TryParse(vVal, out var val))
                builder.AddAttribute(seq++, "Value", val);
            if (node.Attributes.TryGetValue("Count", out var cVal) && int.TryParse(cVal, out var count))
                builder.AddAttribute(seq++, "Count", count);
            if (node.Attributes.TryGetValue("AllowClear", out var acVal))
                builder.AddAttribute(seq++, "AllowClear", ParseBool(acVal));
            if (node.Attributes.TryGetValue("ReadOnly", out var roVal))
                builder.AddAttribute(seq++, "ReadOnly", ParseBool(roVal));
            if (node.Attributes.TryGetValue("Disabled", out var disVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disVal));
            builder.CloseComponent();
        }
        else if (tag.Equals("Slider", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Slider>(seq++);
            if (node.Attributes.TryGetValue("Value", out var vVal) && double.TryParse(vVal, out var val))
                builder.AddAttribute(seq++, "Value", val);
            if (node.Attributes.TryGetValue("Min", out var minVal) && double.TryParse(minVal, out var min))
                builder.AddAttribute(seq++, "Min", min);
            if (node.Attributes.TryGetValue("Max", out var maxVal) && double.TryParse(maxVal, out var max))
                builder.AddAttribute(seq++, "Max", max);
            if (node.Attributes.TryGetValue("Step", out var stepVal) && double.TryParse(stepVal, out var step))
                builder.AddAttribute(seq++, "Step", step);
            if (node.Attributes.TryGetValue("ShowValue", out var svVal))
                builder.AddAttribute(seq++, "ShowValue", ParseBool(svVal));
            if (node.Attributes.TryGetValue("Disabled", out var disVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disVal));
            builder.CloseComponent();
        }
        else if (tag.Equals("ColorPicker", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<ColorPicker>(seq++);
            if (node.Attributes.TryGetValue("Value", out var val))
                builder.AddAttribute(seq++, "Value", val);
            if (node.Attributes.TryGetValue("ShowText", out var stVal))
                builder.AddAttribute(seq++, "ShowText", ParseBool(stVal));
            if (node.Attributes.TryGetValue("Disabled", out var disVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disVal));
            builder.CloseComponent();
        }
        else if (tag.Equals("DatePicker", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<DatePicker>(seq++);
            if (node.Attributes.TryGetValue("Format", out var fVal))
                builder.AddAttribute(seq++, "Format", fVal);
            if (node.Attributes.TryGetValue("Placeholder", out var pVal))
                builder.AddAttribute(seq++, "Placeholder", pVal);
            if (node.Attributes.TryGetValue("AllowClear", out var acVal))
                builder.AddAttribute(seq++, "AllowClear", ParseBool(acVal));
            if (node.Attributes.TryGetValue("Disabled", out var disVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disVal));
            builder.CloseComponent();
        }
        else if (tag.Equals("TimePicker", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<TimePicker>(seq++);
            if (node.Attributes.TryGetValue("Format", out var fVal))
                builder.AddAttribute(seq++, "Format", fVal);
            if (node.Attributes.TryGetValue("ShowSeconds", out var ssVal))
                builder.AddAttribute(seq++, "ShowSeconds", ParseBool(ssVal));
            if (node.Attributes.TryGetValue("AllowClear", out var acVal))
                builder.AddAttribute(seq++, "AllowClear", ParseBool(acVal));
            if (node.Attributes.TryGetValue("Disabled", out var disVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disVal));
            builder.CloseComponent();
        }
        else if (tag.Equals("AutoComplete", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<AutoComplete>(seq++);
            if (node.Attributes.TryGetValue("Placeholder", out var pVal))
                builder.AddAttribute(seq++, "Placeholder", pVal);
            if (node.Attributes.TryGetValue("Value", out var vVal))
                builder.AddAttribute(seq++, "Value", vVal);
            if (node.Attributes.TryGetValue("AllowClear", out var acVal))
                builder.AddAttribute(seq++, "AllowClear", ParseBool(acVal));
            builder.AddAttribute(seq++, "SimpleOptions", new[] { "apple.com", "google.com", "microsoft.com", "tento.dev" });
            builder.CloseComponent();
        }
        // Data Display
        else if (tag.Equals("Avatar", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Avatar>(seq++);
            if (node.Attributes.TryGetValue("Text", out var textVal))
                builder.AddAttribute(seq++, "Text", textVal);
            if (node.Attributes.TryGetValue("Src", out var srcVal))
                builder.AddAttribute(seq++, "Src", srcVal);
            if (node.Attributes.TryGetValue("Size", out var sizeVal))
                builder.AddAttribute(seq++, "Size", ParseSize(sizeVal));
            if (node.Attributes.TryGetValue("Shape", out var shapeVal))
                builder.AddAttribute(seq++, "Shape", shapeVal.Equals("square", StringComparison.OrdinalIgnoreCase) ? AvatarShape.Square : AvatarShape.Circle);
            if (node.Attributes.TryGetValue("Icon", out var iconVal))
                builder.AddAttribute(seq++, "Icon", NormalizeAttributeValue(iconVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else if (tag.Equals("AvatarGroup", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<AvatarGroup>(seq++);
            if (node.Attributes.TryGetValue("MaxCount", out var maxVal) && int.TryParse(maxVal, out var maxCount))
                builder.AddAttribute(seq++, "MaxCount", maxCount);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Badge", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Badge>(seq++);
            if (node.Attributes.TryGetValue("Count", out var countVal) && int.TryParse(countVal, out var count))
                builder.AddAttribute(seq++, "Count", count);
            if (node.Attributes.TryGetValue("Dot", out var dotVal))
                builder.AddAttribute(seq++, "Dot", ParseBool(dotVal));
            if (node.Attributes.TryGetValue("Color", out var colorVal))
                builder.AddAttribute(seq++, "Color", colorVal);
            if (node.Attributes.TryGetValue("Text", out var textVal))
                builder.AddAttribute(seq++, "Text", textVal);
            if (node.Attributes.TryGetValue("OverflowCount", out var ofVal) && int.TryParse(ofVal, out var overflow))
                builder.AddAttribute(seq++, "OverflowCount", overflow);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);

            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Tag", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Tag>(seq++);
            if (node.Attributes.TryGetValue("Color", out var colorVal))
                builder.AddAttribute(seq++, "Color", colorVal);
            if (node.Attributes.TryGetValue("Closable", out var closableVal))
                builder.AddAttribute(seq++, "Closable", ParseBool(closableVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);

            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Card", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Card>(seq++);
            if (node.Attributes.TryGetValue("Title", out var titleVal))
                builder.AddAttribute(seq++, "Title", titleVal);
            if (node.Attributes.TryGetValue("Bordered", out var borderedVal))
                builder.AddAttribute(seq++, "Bordered", ParseBool(borderedVal));
            if (node.Attributes.TryGetValue("Hoverable", out var hoverVal))
                builder.AddAttribute(seq++, "Hoverable", ParseBool(hoverVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);

            var extraNode = node.Children.FirstOrDefault(c => c.TagName.Equals("Extra", StringComparison.OrdinalIgnoreCase));
            if (extraNode != null)
            {
                builder.AddAttribute(seq++, "Extra", (RenderFragment)(eBuilder =>
                {
                    var eSeq = 0;
                    foreach (var ec in extraNode.Children)
                        RenderNode(ec, eBuilder, ref eSeq);
                }));
            }

            var actionsNode = node.Children.FirstOrDefault(c => c.TagName.Equals("Actions", StringComparison.OrdinalIgnoreCase));
            if (actionsNode != null)
            {
                builder.AddAttribute(seq++, "Actions", (RenderFragment)(aBuilder =>
                {
                    var aSeq = 0;
                    foreach (var ac in actionsNode.Children)
                        RenderNode(ac, aBuilder, ref aSeq);
                }));
            }

            var regularChildren = node.Children.Where(c =>
                !c.TagName.Equals("Extra", StringComparison.OrdinalIgnoreCase) &&
                !c.TagName.Equals("Actions", StringComparison.OrdinalIgnoreCase)).ToList();

            var childContentNode = regularChildren.FirstOrDefault(c => c.TagName.Equals("ChildContent", StringComparison.OrdinalIgnoreCase));
            var toRender = childContentNode != null ? childContentNode.Children : regularChildren;

            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in toRender)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Collapse", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Collapse>(seq++);
            if (node.Attributes.TryGetValue("Accordion", out var accVal))
                builder.AddAttribute(seq++, "Accordion", ParseBool(accVal));
            if (node.Attributes.TryGetValue("Bordered", out var bVal))
                builder.AddAttribute(seq++, "Bordered", ParseBool(bVal));
            if (node.Attributes.TryGetValue("Ghost", out var gVal))
                builder.AddAttribute(seq++, "Ghost", ParseBool(gVal));
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("CollapsePanel", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<CollapsePanel>(seq++);
            if (node.Attributes.TryGetValue("Key", out var kVal))
                builder.AddAttribute(seq++, "Key", kVal);
            if (node.Attributes.TryGetValue("Header", out var hVal))
                builder.AddAttribute(seq++, "Header", hVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Descriptions", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Descriptions>(seq++);
            if (node.Attributes.TryGetValue("Title", out var tVal))
                builder.AddAttribute(seq++, "Title", tVal);
            if (node.Attributes.TryGetValue("Bordered", out var bVal))
                builder.AddAttribute(seq++, "Bordered", ParseBool(bVal));
            if (node.Attributes.TryGetValue("Column", out var colVal) && int.TryParse(colVal, out var col))
                builder.AddAttribute(seq++, "Column", col);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("DescriptionsItem", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<DescriptionsItem>(seq++);
            if (node.Attributes.TryGetValue("Label", out var lVal))
                builder.AddAttribute(seq++, "Label", lVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Progress", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Progress>(seq++);
            if (node.Attributes.TryGetValue("Percent", out var pVal) && int.TryParse(pVal, out var percent))
                builder.AddAttribute(seq++, "Percent", percent);
            if (node.Attributes.TryGetValue("ShowInfo", out var infoVal))
                builder.AddAttribute(seq++, "ShowInfo", ParseBool(infoVal));
            if (node.Attributes.TryGetValue("Type", out var typeVal))
                builder.AddAttribute(seq++, "Type", typeVal.Equals("circle", StringComparison.OrdinalIgnoreCase) ? ProgressType.Circle : ProgressType.Line);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else if (tag.Equals("Spin", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Spin>(seq++);
            if (node.Attributes.TryGetValue("Tip", out var tipVal))
                builder.AddAttribute(seq++, "Tip", tipVal);
            if (node.Attributes.TryGetValue("Size", out var sizeVal))
                builder.AddAttribute(seq++, "Size", ParseSize(sizeVal));
            if (node.Attributes.TryGetValue("Spinning", out var spinVal))
                builder.AddAttribute(seq++, "Spinning", ParseBool(spinVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            if (node.Children.Count > 0)
            {
                builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    var cSeq = 0;
                    foreach (var child in node.Children)
                        RenderNode(child, childBuilder, ref cSeq);
                }));
            }
            builder.CloseComponent();
        }
        else if (tag.Equals("Statistic", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Statistic>(seq++);
            if (node.Attributes.TryGetValue("Title", out var titleVal))
                builder.AddAttribute(seq++, "Title", titleVal);
            if (node.Attributes.TryGetValue("Value", out var val))
                builder.AddAttribute(seq++, "Value", val);
            if (node.Attributes.TryGetValue("Precision", out var precVal) && int.TryParse(precVal, out var prec))
                builder.AddAttribute(seq++, "Precision", prec);
            if (node.Attributes.TryGetValue("Prefix", out var prefixVal))
                builder.AddAttribute(seq++, "Prefix", (RenderFragment)(b => b.AddContent(0, prefixVal)));
            if (node.Attributes.TryGetValue("Suffix", out var suffixVal))
                builder.AddAttribute(seq++, "Suffix", (RenderFragment)(b => b.AddContent(0, suffixVal)));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else if (tag.Equals("QRCode", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<QRCode>(seq++);
            if (node.Attributes.TryGetValue("Value", out var val))
                builder.AddAttribute(seq++, "Value", val);
            if (node.Attributes.TryGetValue("Size", out var sizeVal) && int.TryParse(sizeVal, out var size))
                builder.AddAttribute(seq++, "Size", size);
            if (node.Attributes.TryGetValue("Color", out var colVal))
                builder.AddAttribute(seq++, "Color", colVal);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else if (tag.Equals("Empty", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Empty>(seq++);
            if (node.Attributes.TryGetValue("Description", out var descVal))
                builder.AddAttribute(seq++, "Description", descVal);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            if (node.Children.Count > 0)
            {
                builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    var cSeq = 0;
                    foreach (var child in node.Children)
                        RenderNode(child, childBuilder, ref cSeq);
                }));
            }
            builder.CloseComponent();
        }
        else if (tag.Equals("Tooltip", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Tooltip>(seq++);
            if (node.Attributes.TryGetValue("Title", out var titleVal))
                builder.AddAttribute(seq++, "Title", titleVal);
            if (node.Attributes.TryGetValue("Placement", out var plcVal))
                builder.AddAttribute(seq++, "Placement", ParseTooltipPlacement(plcVal));
            if (node.Attributes.TryGetValue("Color", out var colVal))
                builder.AddAttribute(seq++, "Color", colVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Popover", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Popover>(seq++);
            if (node.Attributes.TryGetValue("Title", out var titleVal))
                builder.AddAttribute(seq++, "Title", titleVal);
            if (node.Attributes.TryGetValue("Content", out var contVal))
                builder.AddAttribute(seq++, "Content", contVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Timeline", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Timeline>(seq++);
            if (node.Attributes.TryGetValue("Pending", out var pendingVal))
                builder.AddAttribute(seq++, "Pending", ParseBool(pendingVal));
            if (node.Attributes.TryGetValue("Reverse", out var revVal))
                builder.AddAttribute(seq++, "Reverse", ParseBool(revVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("TimelineItem", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<TimelineItem>(seq++);
            if (node.Attributes.TryGetValue("Color", out var colorVal))
                builder.AddAttribute(seq++, "Color", colorVal);
            if (node.Attributes.TryGetValue("Label", out var labelVal))
                builder.AddAttribute(seq++, "Label", labelVal);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Watermark", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Watermark>(seq++);
            if (node.Attributes.TryGetValue("Content", out var cVal))
                builder.AddAttribute(seq++, "Content", cVal);
            if (node.Attributes.TryGetValue("Rotate", out var rVal) && int.TryParse(rVal, out var rot))
                builder.AddAttribute(seq++, "Rotate", rot);
            if (node.Attributes.TryGetValue("FontSize", out var fsVal) && int.TryParse(fsVal, out var fs))
                builder.AddAttribute(seq++, "FontSize", fs);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                var cSeq = 0;
                foreach (var child in node.Children)
                    RenderNode(child, childBuilder, ref cSeq);
            }));
            builder.CloseComponent();
        }
        else if (tag.Equals("Image", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Image>(seq++);
            if (node.Attributes.TryGetValue("Src", out var srcVal))
                builder.AddAttribute(seq++, "Src", srcVal);
            if (node.Attributes.TryGetValue("Alt", out var altVal))
                builder.AddAttribute(seq++, "Alt", altVal);
            if (node.Attributes.TryGetValue("Width", out var wVal))
                builder.AddAttribute(seq++, "Width", wVal);
            if (node.Attributes.TryGetValue("Height", out var hVal))
                builder.AddAttribute(seq++, "Height", hVal);
            if (node.Attributes.TryGetValue("Preview", out var pVal))
                builder.AddAttribute(seq++, "Preview", ParseBool(pVal));
            builder.CloseComponent();
        }
        // Feedback
        else if (tag.Equals("Alert", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Alert>(seq++);
            if (node.Attributes.TryGetValue("Type", out var typeVal))
                builder.AddAttribute(seq++, "Type", ParseAlertType(typeVal));
            if (node.Attributes.TryGetValue("Message", out var msgVal))
                builder.AddAttribute(seq++, "Message", msgVal);
            if (node.Attributes.TryGetValue("Description", out var descVal))
                builder.AddAttribute(seq++, "Description", descVal);
            if (node.Attributes.TryGetValue("ShowIcon", out var iconVal))
                builder.AddAttribute(seq++, "ShowIcon", ParseBool(iconVal));
            if (node.Attributes.TryGetValue("Closable", out var closableVal))
                builder.AddAttribute(seq++, "Closable", ParseBool(closableVal));
            if (node.Attributes.TryGetValue("Banner", out var bannerVal))
                builder.AddAttribute(seq++, "Banner", ParseBool(bannerVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else if (tag.Equals("Result", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Result>(seq++);
            if (node.Attributes.TryGetValue("Title", out var titleVal))
                builder.AddAttribute(seq++, "Title", titleVal);
            if (node.Attributes.TryGetValue("SubTitle", out var subTitleVal))
                builder.AddAttribute(seq++, "SubTitle", subTitleVal);
            if (node.Attributes.TryGetValue("Status", out var statusVal))
                builder.AddAttribute(seq++, "Status", ParseResultStatus(statusVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);

            var extraNode = node.Children.FirstOrDefault(c => c.TagName.Equals("Extra", StringComparison.OrdinalIgnoreCase));
            if (extraNode != null)
            {
                builder.AddAttribute(seq++, "Extra", (RenderFragment)(eBuilder =>
                {
                    var eSeq = 0;
                    foreach (var ec in extraNode.Children)
                        RenderNode(ec, eBuilder, ref eSeq);
                }));
            }
            builder.CloseComponent();
        }
        else if (tag.Equals("Skeleton", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Skeleton>(seq++);
            if (node.Attributes.TryGetValue("Active", out var activeVal))
                builder.AddAttribute(seq++, "Active", ParseBool(activeVal));
            if (node.Attributes.TryGetValue("Avatar", out var avatarVal))
                builder.AddAttribute(seq++, "Avatar", ParseBool(avatarVal));
            if (node.Attributes.TryGetValue("Paragraph", out var paraVal))
                builder.AddAttribute(seq++, "Paragraph", ParseBool(paraVal));
            if (node.Attributes.TryGetValue("Rows", out var rowsVal) && int.TryParse(rowsVal, out var rows))
                builder.AddAttribute(seq++, "Rows", rows);
            if (node.Attributes.TryGetValue("Round", out var roundVal))
                builder.AddAttribute(seq++, "Round", ParseBool(roundVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else
        {
            // Standard HTML element (div, p, span, button, a, h1-h6, table, thead, tbody, tr, th, td, etc.)
            builder.OpenElement(seq++, tag.ToLowerInvariant());
            foreach (var (k, v) in node.Attributes)
            {
                builder.AddAttribute(seq++, k.ToLowerInvariant(), v);
            }
            foreach (var child in node.Children)
            {
                RenderNode(child, builder, ref seq);
            }
            builder.CloseElement();
        }
    }

    private static ResultStatus ParseResultStatus(string val)
    {
        val = NormalizeEnumString(val);
        return val.ToLowerInvariant() switch
        {
            "success" => ResultStatus.Success,
            "error" => ResultStatus.Error,
            "warning" => ResultStatus.Warning,
            _ => ResultStatus.Info
        };
    }

    private static ButtonType ParseButtonType(string val)
    {
        val = NormalizeEnumString(val);
        return val.ToLowerInvariant() switch
        {
            "default" => ButtonType.Default,
            "dashed" => ButtonType.Dashed,
            "link" => ButtonType.Link,
            "text" => ButtonType.Text,
            _ => ButtonType.Primary
        };
    }

    private static ButtonShape ParseButtonShape(string val)
    {
        val = NormalizeEnumString(val);
        return val.ToLowerInvariant() switch
        {
            "circle" => ButtonShape.Circle,
            "round" => ButtonShape.Round,
            _ => ButtonShape.Default
        };
    }

    private static AlertType ParseAlertType(string val)
    {
        val = NormalizeEnumString(val);
        return val.ToLowerInvariant() switch
        {
            "success" => AlertType.Success,
            "warning" => AlertType.Warning,
            "error" => AlertType.Error,
            _ => AlertType.Info
        };
    }

    private static TypographyType ParseTypographyType(string val)
    {
        val = NormalizeEnumString(val);
        return val.ToLowerInvariant() switch
        {
            "secondary" => TypographyType.Secondary,
            "success" => TypographyType.Success,
            "warning" => TypographyType.Warning,
            "danger" => TypographyType.Danger,
            _ => TypographyType.Default
        };
    }

    private static SpaceDirection ParseSpaceDirection(string val)
    {
        val = NormalizeEnumString(val);
        return val.ToLowerInvariant().Contains("vert") ? SpaceDirection.Vertical : SpaceDirection.Horizontal;
    }

    private static StepsDirection ParseStepsDirection(string val)
    {
        val = NormalizeEnumString(val);
        return val.ToLowerInvariant().Contains("vert") ? StepsDirection.Vertical : StepsDirection.Horizontal;
    }

    private static TabType ParseTabType(string val)
    {
        val = NormalizeEnumString(val);
        return val.ToLowerInvariant().Contains("card") ? TabType.Card : TabType.Line;
    }

    private static TooltipPlacement ParseTooltipPlacement(string val)
    {
        val = NormalizeEnumString(val);
        return val.ToLowerInvariant() switch
        {
            "bottom" => TooltipPlacement.Bottom,
            "left" => TooltipPlacement.Left,
            "right" => TooltipPlacement.Right,
            _ => TooltipPlacement.Top
        };
    }

    private static ComponentSize ParseSize(string val)
    {
        val = NormalizeEnumString(val);
        return val.ToLowerInvariant() switch
        {
            "small" => ComponentSize.Small,
            "large" => ComponentSize.Large,
            _ => ComponentSize.Middle
        };
    }

    private static string NormalizeEnumString(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        val = val.Trim();
        if (val.StartsWith('@')) val = val[1..].Trim();
        if (val.Contains('.')) val = val[(val.LastIndexOf('.') + 1)..];
        return val;
    }

    private static bool ParseBool(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return true;
        val = val.Trim();
        if (val.StartsWith('@')) val = val[1..].Trim();
        return bool.TryParse(val, out var b) ? b : val.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
