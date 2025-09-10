using KeyLens.Api.Services;

namespace KeyLens;

public interface INotificationDestination
{
    Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken);
}
