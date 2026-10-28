using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TentoUI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTentoUI(this IServiceCollection services, Action<TentoTheme>? configureTheme = null)
    {
        var theme = new TentoTheme();
        configureTheme?.Invoke(theme);
        services.TryAddSingleton(theme);

        services.TryAddScoped<IModalService, ModalService>();
        services.TryAddScoped<IMessageService, MessageService>();
        services.TryAddScoped<INotificationService, NotificationService>();

        return services;
    }
}
