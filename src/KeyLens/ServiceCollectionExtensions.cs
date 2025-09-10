using KeyLens;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationBroker(this IServiceCollection services) =>
        services.AddSingleton<INotificationBroker, NotificationBroker>();
}
