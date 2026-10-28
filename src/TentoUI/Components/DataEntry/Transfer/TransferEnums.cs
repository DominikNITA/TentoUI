namespace TentoUI;

public class TransferItem
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Disabled { get; set; }

    public TransferItem() { }

    public TransferItem(string key, string title, string? description = null, bool disabled = false)
    {
        Key = key;
        Title = title;
        Description = description;
        Disabled = disabled;
    }
}
