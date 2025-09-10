using KeyLens.Api.Services;

namespace KeyLens;

internal class NotificationBroker(IEnumerable<INotificationDestination> destinations) : INotificationBroker
{
    public Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
    {
        var tasks = destinations.Select(destination => destination.SendNotificationAsync(notification, cancellationToken));
        return Task.WhenAll(tasks);
    }
}
