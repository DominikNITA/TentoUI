using Microsoft.AspNetCore.Components;

namespace TentoUI;

public enum MessageType
{
    Info,
    Success,
    Warning,
    Error,
    Loading
}

public class MessageOptions
{
    public string Content { get; set; } = string.Empty;
    public RenderFragment? ContentTemplate { get; set; }
    public MessageType Type { get; set; } = MessageType.Info;
    public double DurationSeconds { get; set; } = 3.0;
    public Action? OnClose { get; set; }
}

public interface IMessageRef
{
    Guid Id { get; }
    void Close();
    void Update(string content, MessageType? type = null, double? durationSeconds = null);
}

public class MessageItem : IMessageRef
{
    private readonly MessageService _service;
    private readonly object _itemLock = new();
    private CancellationTokenSource? _cts;
    private bool _isClosed;

    public Guid Id { get; } = Guid.NewGuid();
    public string Content { get; private set; }
    public RenderFragment? ContentTemplate { get; private set; }
    public MessageType Type { get; private set; }
    public double DurationSeconds { get; private set; }
    public Action? OnClose { get; }

    public MessageItem(MessageOptions options, MessageService service)
    {
        _service = service;
        Content = options.Content;
        ContentTemplate = options.ContentTemplate;
        Type = options.Type;
        DurationSeconds = options.DurationSeconds;
        OnClose = options.OnClose;

        StartTimer();
    }

    private void StartTimer()
    {
        if (DurationSeconds <= 0) return;
        lock (_itemLock)
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
        lock (_itemLock)
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
        _service.Remove(Id);
    }

    public void Update(string content, MessageType? type = null, double? durationSeconds = null)
    {
        lock (_itemLock)
        {
            if (_isClosed) return;

            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
            }
            catch { }
            _cts = null;

            Content = content;
            if (type.HasValue) Type = type.Value;
            if (durationSeconds.HasValue) DurationSeconds = durationSeconds.Value;

            StartTimer();
        }

        _service.NotifyChange();
    }
}

public interface IMessageService
{
    event Action? OnChange;
    IReadOnlyList<MessageItem> Messages { get; }

    IMessageRef Success(string content, double durationSeconds = 3, Action? onClose = null);
    IMessageRef Error(string content, double durationSeconds = 3, Action? onClose = null);
    IMessageRef Info(string content, double durationSeconds = 3, Action? onClose = null);
    IMessageRef Warning(string content, double durationSeconds = 3, Action? onClose = null);
    IMessageRef Loading(string content, double durationSeconds = 0, Action? onClose = null);

    IMessageRef Open(MessageOptions options);
    void Destroy();
}

public class MessageService : IMessageService
{
    private readonly object _lock = new();
    private readonly System.Collections.Generic.List<MessageItem> _messages = new();
    public event Action? OnChange;

    public IReadOnlyList<MessageItem> Messages
    {
        get
        {
            lock (_lock)
            {
                return _messages.ToList();
            }
        }
    }

    public IMessageRef Open(MessageOptions options)
    {
        var item = new MessageItem(options, this);
        lock (_lock)
        {
            _messages.Add(item);
        }
        OnChange?.Invoke();
        return item;
    }

    public IMessageRef Success(string content, double durationSeconds = 3, Action? onClose = null) =>
        Open(new MessageOptions { Content = content, Type = MessageType.Success, DurationSeconds = durationSeconds, OnClose = onClose });

    public IMessageRef Error(string content, double durationSeconds = 3, Action? onClose = null) =>
        Open(new MessageOptions { Content = content, Type = MessageType.Error, DurationSeconds = durationSeconds, OnClose = onClose });

    public IMessageRef Info(string content, double durationSeconds = 3, Action? onClose = null) =>
        Open(new MessageOptions { Content = content, Type = MessageType.Info, DurationSeconds = durationSeconds, OnClose = onClose });

    public IMessageRef Warning(string content, double durationSeconds = 3, Action? onClose = null) =>
        Open(new MessageOptions { Content = content, Type = MessageType.Warning, DurationSeconds = durationSeconds, OnClose = onClose });

    public IMessageRef Loading(string content, double durationSeconds = 0, Action? onClose = null) =>
        Open(new MessageOptions { Content = content, Type = MessageType.Loading, DurationSeconds = durationSeconds, OnClose = onClose });

    internal void Remove(Guid id)
    {
        bool removed = false;
        lock (_lock)
        {
            var item = _messages.FirstOrDefault(m => m.Id == id);
            if (item != null)
            {
                _messages.Remove(item);
                removed = true;
            }
        }

        if (removed)
        {
            OnChange?.Invoke();
        }
    }

    internal void NotifyChange() => OnChange?.Invoke();

    public void Destroy()
    {
        lock (_lock)
        {
            _messages.Clear();
        }
        OnChange?.Invoke();
    }
}
