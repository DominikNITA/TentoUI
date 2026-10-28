using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using TentoUI;

namespace TentoUI.Docs.Components;

/// <summary>
/// Dynamically parses and renders Razor/HTML snippets for TentoUI interactive live demos.
/// Enables users to modify example code in the browser and see real-time updates.
/// </summary>
public class TentoMarkupRenderer : ComponentBase
{
    [Parameter] public string? Markup { get; set; }
    [Parameter] public RenderFragment? Fallback { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(Markup))
        {
            Fallback?.Invoke(builder);
            return;
        }

        try
        {
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
        catch
        {
            // If syntax is invalid while typing, keep fallback or render notice
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

    public static System.Collections.Generic.List<MarkupNode> ParseNodes(string input)
    {
        var result = new System.Collections.Generic.List<MarkupNode>();
        if (string.IsNullOrWhiteSpace(input)) return result;

        var pos = 0;
        var len = input.Length;

        while (pos < len)
        {
            var nextLt = input.IndexOf('<', pos);
            if (nextLt == -1)
            {
                var text = input[pos..].Trim();
                if (!string.IsNullOrEmpty(text))
                    result.Add(new MarkupNode { TextContent = text });
                break;
            }

            if (nextLt > pos)
            {
                var text = input[pos..nextLt].Trim();
                if (!string.IsNullOrEmpty(text))
                    result.Add(new MarkupNode { TextContent = text });
            }

            pos = nextLt;

            // Check comments
            if (pos + 3 < len && input.Substring(pos, 4) == "<!--")
            {
                var commentEnd = input.IndexOf("-->", pos + 4);
                pos = (commentEnd == -1) ? len : commentEnd + 3;
                continue;
            }

            // Check Razor comments @* ... *@
            if (pos + 1 < len && input.Substring(pos, 2) == "@*")
            {
                var commentEnd = input.IndexOf("*@", pos + 2);
                pos = (commentEnd == -1) ? len : commentEnd + 2;
                continue;
            }

            // Find tag end
            var nextGt = input.IndexOf('>', pos);
            if (nextGt == -1) break;

            var tagHeader = input.Substring(pos + 1, nextGt - pos - 1).Trim();
            var isSelfClosing = tagHeader.EndsWith('/');
            if (isSelfClosing)
                tagHeader = tagHeader[..^1].Trim();

            // Ignore closing tag here if orphaned
            if (tagHeader.StartsWith('/'))
            {
                pos = nextGt + 1;
                continue;
            }

            var parts = ParseTagHeader(tagHeader);
            if (string.IsNullOrEmpty(parts.TagName))
            {
                pos = nextGt + 1;
                continue;
            }

            var node = new MarkupNode
            {
                TagName = parts.TagName,
                Attributes = parts.Attributes
            };

            pos = nextGt + 1;

            if (isSelfClosing || IsVoidElement(parts.TagName))
            {
                result.Add(node);
                continue;
            }

            // Find closing tag </parts.TagName>
            var closingTag = $"</{parts.TagName}>";
            var closingIdx = input.IndexOf(closingTag, pos, StringComparison.OrdinalIgnoreCase);
            if (closingIdx != -1)
            {
                var innerMarkup = input[pos..closingIdx];
                node.Children = ParseNodes(innerMarkup);
                pos = closingIdx + closingTag.Length;
            }
            else
            {
                // Fallback: parse rest as children
                var rest = input[pos..];
                node.Children = ParseNodes(rest);
                pos = len;
            }

            result.Add(node);
        }

        return result;
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

        var rest = header[tagName.Length..].Trim();
        var attrMatches = Regex.Matches(rest, @"([a-zA-Z0-9_\-\.:]+)(?:=(?:""([^""]*)""|'([^']*)'|([^\s>]+)))?");

        foreach (Match m in attrMatches)
        {
            var key = m.Groups[1].Value;
            var val = m.Groups[2].Success ? m.Groups[2].Value
                    : m.Groups[3].Success ? m.Groups[3].Value
                    : m.Groups[4].Success ? m.Groups[4].Value
                    : "true"; // bare attribute flag

            attrs[key] = val;
        }

        return (tagName, attrs);
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
            if (node.Attributes.TryGetValue("Extra", out var extraVal))
                builder.AddAttribute(seq++, "Extra", extraVal);
            if (node.Attributes.TryGetValue("Bordered", out var borderedVal))
                builder.AddAttribute(seq++, "Bordered", ParseBool(borderedVal));
            if (node.Attributes.TryGetValue("Hoverable", out var hoverVal))
                builder.AddAttribute(seq++, "Hoverable", ParseBool(hoverVal));
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
        else if (tag.Equals("Progress", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Progress>(seq++);
            if (node.Attributes.TryGetValue("Percent", out var pVal) && int.TryParse(pVal, out var percent))
                builder.AddAttribute(seq++, "Percent", percent);
            if (node.Attributes.TryGetValue("ShowInfo", out var infoVal))
                builder.AddAttribute(seq++, "ShowInfo", ParseBool(infoVal));
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
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else if (tag.Equals("Switch", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Switch>(seq++);
            if (node.Attributes.TryGetValue("Checked", out var checkedVal))
                builder.AddAttribute(seq++, "Checked", ParseBool(checkedVal));
            if (node.Attributes.TryGetValue("Disabled", out var disabledVal))
                builder.AddAttribute(seq++, "Disabled", ParseBool(disabledVal));
            if (node.Attributes.TryGetValue("Size", out var sizeVal))
                builder.AddAttribute(seq++, "Size", ParseSize(sizeVal));
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
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
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
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
        else if (tag.Equals("Space", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Space>(seq++);
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
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
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
        else if (tag.Equals("Timeline", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Timeline>(seq++);
            if (node.Attributes.TryGetValue("Pending", out var pendingVal))
                builder.AddAttribute(seq++, "Pending", ParseBool(pendingVal));
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
        else if (tag.Equals("Statistic", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Statistic>(seq++);
            if (node.Attributes.TryGetValue("Title", out var titleVal))
                builder.AddAttribute(seq++, "Title", titleVal);
            if (node.Attributes.TryGetValue("Value", out var val))
                builder.AddAttribute(seq++, "Value", val);
            if (node.Attributes.TryGetValue("Prefix", out var prefixVal))
                builder.AddAttribute(seq++, "Prefix", prefixVal);
            if (node.Attributes.TryGetValue("Suffix", out var suffixVal))
                builder.AddAttribute(seq++, "Suffix", suffixVal);
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
            if (node.Attributes.TryGetValue("Bordered", out var bVal))
                builder.AddAttribute(seq++, "Bordered", ParseBool(bVal));
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
            builder.CloseComponent();
        }
        else if (tag.Equals("Icon", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<Icon>(seq++);
            if (node.Attributes.TryGetValue("Name", out var nameVal))
            {
                if (nameVal.StartsWith("IconNames.", StringComparison.OrdinalIgnoreCase))
                    nameVal = nameVal[10..].ToLowerInvariant();
                builder.AddAttribute(seq++, "Name", nameVal);
            }
            if (node.Attributes.TryGetValue("Size", out var sizeVal) && int.TryParse(sizeVal, out var size))
                builder.AddAttribute(seq++, "Size", size);
            if (node.Attributes.TryGetValue("Class", out var classVal))
                builder.AddAttribute(seq++, "Class", classVal);
            builder.CloseComponent();
        }
        else
        {
            // Standard HTML element (div, p, span, button, a, h1-h6, etc.)
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
        if (val.Contains('.')) val = val[(val.LastIndexOf('.') + 1)..];
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
        if (val.Contains('.')) val = val[(val.LastIndexOf('.') + 1)..];
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
        if (val.Contains('.')) val = val[(val.LastIndexOf('.') + 1)..];
        return val.ToLowerInvariant() switch
        {
            "circle" => ButtonShape.Circle,
            "round" => ButtonShape.Round,
            _ => ButtonShape.Default
        };
    }

    private static AlertType ParseAlertType(string val)
    {
        if (val.Contains('.')) val = val[(val.LastIndexOf('.') + 1)..];
        return val.ToLowerInvariant() switch
        {
            "success" => AlertType.Success,
            "warning" => AlertType.Warning,
            "error" => AlertType.Error,
            _ => AlertType.Info
        };
    }

    private static ComponentSize ParseSize(string val)
    {
        if (val.Contains('.')) val = val[(val.LastIndexOf('.') + 1)..];
        return val.ToLowerInvariant() switch
        {
            "small" => ComponentSize.Small,
            "large" => ComponentSize.Large,
            _ => ComponentSize.Middle
        };
    }

    private static bool ParseBool(string val) =>
        bool.TryParse(val, out var b) ? b : val.Equals("true", StringComparison.OrdinalIgnoreCase);
}
