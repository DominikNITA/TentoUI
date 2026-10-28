using Microsoft.AspNetCore.Components;

namespace TentoUI;

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    None
}

public enum NotificationPlacement
{
    TopRight,
    TopLeft,
    BottomRight,
    BottomLeft
}

public class NotificationOptions
{
    public string? Key { get; set; }
    public string Title { get; set; } = string.Empty;
    public RenderFragment? TitleTemplate { get; set; }
    public string? Description { get; set; }
    public RenderFragment? DescriptionTemplate { get; set; }
    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationPlacement Placement { get; set; } = NotificationPlacement.TopRight;
    public double DurationSeconds { get; set; } = 4.5;
    public RenderFragment? Icon { get; set; }
    public RenderFragment? Btn { get; set; }
    public Action? OnClose { get; set; }
}

public class NotificationItem
{
    private readonly NotificationService _service;
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private bool _isClosed;

    public string Key { get; }
    public string Title { get; set; }
    public RenderFragment? TitleTemplate { get; set; }
    public string? Description { get; set; }
    public RenderFragment? DescriptionTemplate { get; set; }
    public NotificationType Type { get; set; }
    public NotificationPlacement Placement { get; set; }
    public double DurationSeconds { get; set; }
    public RenderFragment? Icon { get; set; }
    public RenderFragment? Btn { get; set; }
    public Action? OnClose { get; }

    public NotificationItem(NotificationOptions options, NotificationService service)
    {
        _service = service;
        Key = !string.IsNullOrEmpty(options.Key) ? options.Key : Guid.NewGuid().ToString("N");
        Title = options.Title;
        TitleTemplate = options.TitleTemplate;
        Description = options.Description;
        DescriptionTemplate = options.DescriptionTemplate;
        Type = options.Type;
        Placement = options.Placement;
        DurationSeconds = options.DurationSeconds;
        Icon = options.Icon;
        Btn = options.Btn;
        OnClose = options.OnClose;

        StartTimer();
    }

    private void StartTimer()
    {
        if (DurationSeconds <= 0) return;
        lock (_lock)
        {
            if (_isClosed) return;
            _cts = new CancellationTokenSource();
            _ = Task.Delay(TimeSpan.FromSeconds(DurationSeconds), _cts.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        Close();
                    }
                }, TaskScheduler.Default);
        }
    }

    public void Close()
    {
        Action? onCloseAction = null;
        lock (_lock)
        {
            if (_isClosed) return;
            _isClosed = true;
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
            }
            catch { }
            _cts = null;
            onCloseAction = OnClose;
        }

        onCloseAction?.Invoke();
        _service.Remove(Key);
    }
}

public interface INotificationService
{
    event Action? OnChange;
    IReadOnlyList<NotificationItem> Notifications { get; }

    NotificationItem Open(NotificationOptions options);
    NotificationItem Success(string title, string? description = null, double duration = 4.5, NotificationPlacement placement = NotificationPlacement.TopRight, Action? onClose = null);
    NotificationItem Info(string title, string? description = null, double duration = 4.5, NotificationPlacement placement = NotificationPlacement.TopRight, Action? onClose = null);
    NotificationItem Warning(string title, string? description = null, double duration = 4.5, NotificationPlacement placement = NotificationPlacement.TopRight, Action? onClose = null);
    NotificationItem Error(string title, string? description = null, double duration = 4.5, NotificationPlacement placement = NotificationPlacement.TopRight, Action? onClose = null);

    void Close(string key);
    void Destroy();
}

public class NotificationService : INotificationService
{
    private readonly object _lock = new();
    private readonly System.Collections.Generic.List<NotificationItem> _notifications = new();
    public event Action? OnChange;

    public IReadOnlyList<NotificationItem> Notifications
    {
        get
        {
            lock (_lock)
            {
                return _notifications.ToList();
            }
        }
    }

    public NotificationItem Open(NotificationOptions options)
    {
        NotificationItem item;
        lock (_lock)
        {
            if (!string.IsNullOrEmpty(options.Key))
            {
                var existing = _notifications.FirstOrDefault(n => n.Key == options.Key);
                if (existing != null)
                {
                    existing.Close();
                }
            }
            item = new NotificationItem(options, this);
            _notifications.Add(item);
        }
        OnChange?.Invoke();
        return item;
    }

    public NotificationItem Success(string title, string? description = null, double duration = 4.5, NotificationPlacement placement = NotificationPlacement.TopRight, Action? onClose = null) =>
        Open(new NotificationOptions { Title = title, Description = description, Type = NotificationType.Success, DurationSeconds = duration, Placement = placement, OnClose = onClose });

    public NotificationItem Info(string title, string? description = null, double duration = 4.5, NotificationPlacement placement = NotificationPlacement.TopRight, Action? onClose = null) =>
        Open(new NotificationOptions { Title = title, Description = description, Type = NotificationType.Info, DurationSeconds = duration, Placement = placement, OnClose = onClose });

    public NotificationItem Warning(string title, string? description = null, double duration = 4.5, NotificationPlacement placement = NotificationPlacement.TopRight, Action? onClose = null) =>
        Open(new NotificationOptions { Title = title, Description = description, Type = NotificationType.Warning, DurationSeconds = duration, Placement = placement, OnClose = onClose });

    public NotificationItem Error(string title, string? description = null, double duration = 4.5, NotificationPlacement placement = NotificationPlacement.TopRight, Action? onClose = null) =>
        Open(new NotificationOptions { Title = title, Description = description, Type = NotificationType.Error, DurationSeconds = duration, Placement = placement, OnClose = onClose });

    public void Close(string key)
    {
        NotificationItem? target;
        lock (_lock)
        {
            target = _notifications.FirstOrDefault(n => n.Key == key);
        }
        target?.Close();
    }

    internal void Remove(string key)
    {
        bool removed = false;
        lock (_lock)
        {
            var item = _notifications.FirstOrDefault(n => n.Key == key);
            if (item != null)
            {
                _notifications.Remove(item);
                removed = true;
            }
        }
        if (removed)
        {
            OnChange?.Invoke();
        }
    }

    public void Destroy()
    {
        lock (_lock)
        {
            _notifications.Clear();
        }
        OnChange?.Invoke();
    }
}
