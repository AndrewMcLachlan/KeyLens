using KeyLens.Entities;

namespace KeyLens.Api.Services;

public class MonitorService(IServiceScopeFactory scopeFactory, ILogger<MonitorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DoWorkAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IConnectionRepository>();
        var scanner = scope.ServiceProvider.GetRequiredService<ConnectionScanner>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationBroker = scope.ServiceProvider.GetRequiredService<INotificationBroker>();

        var connections = (await repository.GetAllEnabledAsync(cancellationToken)).ToList();

        logger.LogInformation("Scanning {count} enabled connections", connections.Count);

        var scannedAt = DateTimeOffset.UtcNow;
        var credentials = new List<CredentialRecord>();

        foreach (var connection in connections)
        {
            var result = await scanner.ScanAsync(connection, cancellationToken);

            if (result.IsSuccess)
            {
                connection.RecordScanSuccess(scannedAt);
                credentials.AddRange(result.Credentials);
            }
            else
            {
                connection.RecordScanFailure(scannedAt, result.Error!);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var expiring = credentials
            .Where(c => c.ExpiresOn.HasValue && (
                c.ExpiresOn.Value.UtcDateTime.Date == DateTime.UtcNow.Date.AddDays(30) ||
                c.ExpiresOn.Value.UtcDateTime.Date == DateTime.UtcNow.Date.AddDays(7) ||
                c.ExpiresOn.Value.UtcDateTime.Date <= DateTime.UtcNow.Date.AddDays(3)))
            .DistinctBy(c => c.CredentialId)
            .ToList();

        logger.LogInformation("Discovered {count} expiring/expired credentials", expiring.Count);

        foreach (var credential in expiring)
        {
            await notificationBroker.SendNotificationAsync(new Notification(credential), cancellationToken);
        }
    }
}
