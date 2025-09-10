namespace KeyLens.Api.Services;

public class MonitorService(
[FromKeyedServices(ServiceKeys.Universal)] IEnumerable<ICredentialProvider> credentialProviders,
INotificationBroker notificationBroker,
ILogger<MonitorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Modern approach with PeriodicTimer
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
            // Service is stopping
        }
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        var results = new List<CredentialRecord>();

        var tasks = credentialProviders.Select(async provider =>
        {
            var providerResults = new List<CredentialRecord>();
            await foreach (var credential in provider.EnumerateAsync(cancellationToken))
            {
                providerResults.Add(credential);
            }
            return providerResults;
        });

        var allProviderResults = await Task.WhenAll(tasks);

        foreach (var providerResults in allProviderResults)
        {
            results.AddRange(providerResults);
        }

        var cleanedResults = results
            .Where(c => c.ExpiresOn.HasValue && (
            c.ExpiresOn.Value.UtcDateTime.Date == DateTime.UtcNow.Date.AddDays(30) ||
            c.ExpiresOn.Value.UtcDateTime.Date == DateTime.UtcNow.Date.AddDays(7) ||
            c.ExpiresOn.Value.UtcDateTime.Date <= DateTime.UtcNow.Date.AddDays(3)))
            .DistinctBy(c => c.CredentialId);

        logger.LogInformation("Discovered {count} expiring/expired credentials", cleanedResults.Count());

        foreach (var credential in cleanedResults)
        {
            await notificationBroker.SendNotificationAsync(new Notification(credential), cancellationToken);
        }


        // Your recurring work here
        await Task.Delay(100, cancellationToken); // Simulate work
    }
}
