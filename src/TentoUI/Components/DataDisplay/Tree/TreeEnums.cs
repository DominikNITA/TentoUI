namespace TentoUI;

public class TreeDataNode
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
    public bool IsLeaf { get; set; }
    public System.Collections.Generic.List<TreeDataNode> Children { get; set; } = new();

    public TreeDataNode() { }

    public TreeDataNode(string key, string title, IEnumerable<TreeDataNode>? children = null, bool isLeaf = false)
    {
        Key = key;
        Title = title;
        IsLeaf = isLeaf;
        if (children != null)
        {
            Children.AddRange(children);
        }
    }
}
