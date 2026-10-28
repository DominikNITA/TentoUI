using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TentoUI;
using TentoUI.Configuration;

namespace TentoUI.Extensibility;

public abstract class TentoInputBase<TValue> : InputBase<TValue>
{
    [Parameter] public string? Id { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public ComponentSize? Size { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public ComponentStatus Status { get; set; } = ComponentStatus.None;

    [CascadingParameter] public TentoConfigContext? ConfigContext { get; set; }
    [CascadingParameter] public FormContext? FormContext { get; set; }
    [CascadingParameter] public FormItemContext? FormItemContext { get; set; }

    protected string ElementId { get; set; } = string.Empty;
    protected bool IsDisabled => Disabled || (FormContext?.Disabled ?? false);

    protected ComponentSize ResolvedSize(ComponentSize? explicitSize) =>
        explicitSize ?? ConfigContext?.ComponentSize ?? ComponentSize.Middle;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<FormItemContext>(nameof(FormItemContext), out var formItemCtx))
        {
            FormItemContext = formItemCtx;
        }
        if (parameters.TryGetValue<FormContext>(nameof(FormContext), out var formCtx))
        {
            FormContext = formCtx;
        }
        if (parameters.TryGetValue<TentoConfigContext>(nameof(ConfigContext), out var configCtx))
        {
            ConfigContext = configCtx;
        }

        if (parameters.TryGetValue<Expression<Func<TValue>>>(nameof(ValueExpression), out var expr) && expr != null)
        {
            return base.SetParametersAsync(parameters);
        }

        var dict = new Dictionary<string, object?>();
        foreach (var param in parameters)
        {
            dict[param.Name] = param.Value;
        }

        dict[nameof(ValueExpression)] = (Expression<Func<TValue>>)(() => Value!);
        dict.Remove(nameof(FormItemContext));
        dict.Remove(nameof(FormContext));
        dict.Remove(nameof(ConfigContext));

        return base.SetParametersAsync(ParameterView.FromDictionary(dict!));
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SyncElementId();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        SyncElementId();
    }

    private void SyncElementId()
    {
        if (!string.IsNullOrEmpty(Id))
        {
            ElementId = Id;
        }
        else if (FormItemContext != null && !string.IsNullOrEmpty(FormItemContext.Id))
        {
            ElementId = FormItemContext.Id;
        }
        else if (string.IsNullOrEmpty(ElementId))
        {
            ElementId = $"tento-input-{Guid.NewGuid():N}";
        }

        if (FormItemContext != null && string.IsNullOrEmpty(FormItemContext.Id))
        {
            FormItemContext.Id = ElementId;
        }
    }

    protected ComponentStatus EffectiveStatus
    {
        get
        {
            if (Status != ComponentStatus.None) return Status;
            if (EditContext != null && FieldIdentifier.FieldName != null)
            {
                var messages = EditContext.GetValidationMessages(FieldIdentifier);
                if (messages.Any()) return ComponentStatus.Error;
            }
            return FormItemContext?.ValidateStatus ?? ComponentStatus.None;
        }
    }

    protected string GetStatusBorderClass() => EffectiveStatus switch
    {
        ComponentStatus.Error => "border-red-500 focus:border-red-500 focus:ring-red-500/20 text-red-900 placeholder-red-300",
        ComponentStatus.Warning => "border-amber-500 focus:border-amber-500 focus:ring-amber-500/20 text-amber-900",
        ComponentStatus.Success => "border-emerald-500 focus:border-emerald-500 focus:ring-emerald-500/20",
        _ => "border-slate-300 dark:border-slate-600 focus:border-primary-500 focus:ring-primary-500/20"
    };
}
