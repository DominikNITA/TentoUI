namespace TentoUI;

public enum UploadStatus
{
    Uploading,
    Done,
    Error,
    Removed
}

public class UploadFileItem
{
    public string Uid { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? ContentType { get; set; }
    public UploadStatus Status { get; set; } = UploadStatus.Done;
    public int Percent { get; set; } = 100;
    public string? ErrorMessage { get; set; }
    public string? ThumbUrl { get; set; }
}
