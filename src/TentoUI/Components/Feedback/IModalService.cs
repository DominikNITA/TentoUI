using Microsoft.AspNetCore.Components;

namespace TentoUI;

public enum ModalType
{
    Confirm,
    Info,
    Success,
    Warning,
    Error
}

public class ModalConfirmOptions
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public RenderFragment? ContentTemplate { get; set; }
    public string OkText { get; set; } = "OK";
    public string CancelText { get; set; } = "Cancel";
    public ButtonType OkType { get; set; } = ButtonType.Primary;
    public bool OkDanger { get; set; }
    public ModalType Type { get; set; } = ModalType.Confirm;
    public bool MaskClosable { get; set; } = false;
    public string Width { get; set; } = "416px";
}

public class ModalInstance
{
    public Guid Id { get; } = Guid.NewGuid();
    public ModalConfirmOptions Options { get; }
    public TaskCompletionSource<bool> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ModalInstance(ModalConfirmOptions options)
    {
        Options = options;
    }
}

public interface IModalService
{
    event Action? OnModalChanged;
    IReadOnlyList<ModalInstance> ActiveModals { get; }

    Task<bool> ConfirmAsync(ModalConfirmOptions options);
    Task<bool> ConfirmAsync(string title, string content, ModalType type = ModalType.Confirm);
    Task InfoAsync(string title, string content);
    Task SuccessAsync(string title, string content);
    Task WarningAsync(string title, string content);
    Task ErrorAsync(string title, string content);
    void ResolveModal(Guid id, bool result)
    {
        var instance = ActiveModals.FirstOrDefault(m => m.Id == id);
        if (instance != null)
        {
            instance.Tcs.TrySetResult(result);
        }
    }
    void ResolveModal(ModalInstance instance, bool result) => ResolveModal(instance.Id, result);
    void CloseAll();
}

public class ModalService : IModalService
{
    private readonly System.Collections.Generic.List<ModalInstance> _activeModals = new();

    public event Action? OnModalChanged;
    public IReadOnlyList<ModalInstance> ActiveModals => _activeModals.AsReadOnly();

    public Task<bool> ConfirmAsync(ModalConfirmOptions options)
    {
        var instance = new ModalInstance(options);
        _activeModals.Add(instance);
        OnModalChanged?.Invoke();
        return instance.Tcs.Task;
    }

    public Task<bool> ConfirmAsync(string title, string content, ModalType type = ModalType.Confirm)
    {
        return ConfirmAsync(new ModalConfirmOptions
        {
            Title = title,
            Content = content,
            Type = type,
            OkType = ButtonType.Primary,
            OkDanger = (type == ModalType.Error || type == ModalType.Warning)
        });
    }

    public async Task InfoAsync(string title, string content) =>
        await ConfirmAsync(new ModalConfirmOptions { Title = title, Content = content, Type = ModalType.Info, CancelText = string.Empty });

    public async Task SuccessAsync(string title, string content) =>
        await ConfirmAsync(new ModalConfirmOptions { Title = title, Content = content, Type = ModalType.Success, CancelText = string.Empty });

    public async Task WarningAsync(string title, string content) =>
        await ConfirmAsync(new ModalConfirmOptions { Title = title, Content = content, Type = ModalType.Warning, CancelText = string.Empty, OkDanger = true });

    public async Task ErrorAsync(string title, string content) =>
        await ConfirmAsync(new ModalConfirmOptions { Title = title, Content = content, Type = ModalType.Error, CancelText = string.Empty, OkDanger = true });

    public void ResolveModal(Guid id, bool result)
    {
        var instance = _activeModals.FirstOrDefault(m => m.Id == id);
        if (instance != null)
        {
            _activeModals.Remove(instance);
            instance.Tcs.TrySetResult(result);
            OnModalChanged?.Invoke();
        }
    }

    public void ResolveModal(ModalInstance instance, bool result) => ResolveModal(instance.Id, result);

    public void CloseAll()
    {
        foreach (var modal in _activeModals.ToList())
        {
            modal.Tcs.TrySetResult(false);
        }
        _activeModals.Clear();
        OnModalChanged?.Invoke();
    }
}
