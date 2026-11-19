using KeyLens.Entities;
using KeyLens.Security;
using Microsoft.Extensions.Logging;

namespace KeyLens;

/// <summary>
/// Enumerates credentials for stored connections, decrypting each connection's secret and handing it
/// to the matching provider factory.
/// </summary>
public sealed class ConnectionScanner(
    IEnumerable<ICredentialProviderFactory> factories,
    IConnectionSecretStore secretStore,
    ILogger<ConnectionScanner> logger)
{
    public async Task<ConnectionScanResult> ScanAsync(Connection connection, CancellationToken cancellationToken = default)
    {
        var matching = factories.Where(f => f.Type == connection.Type).ToList();

        if (matching.Count == 0)
        {
            return ConnectionScanResult.Failed(connection, $"No provider is registered for {connection.Type}.");
        }

        try
        {
            var secret = connection.SecretName is null
                ? null
                : await secretStore.GetAsync(connection.SecretName, cancellationToken);

            var records = new List<CredentialRecord>();

            foreach (var provider in matching.SelectMany(f => f.Create(connection, secret)))
            {
                await foreach (var record in provider.EnumerateAsync(cancellationToken))
                {
                    records.Add(record);
                }
            }

            return ConnectionScanResult.Succeeded(connection, records);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Scan failed for connection {ConnectionId} ({ConnectionName})", connection.Id, connection.Name);
            return ConnectionScanResult.Failed(connection, ex.Message);
        }
    }
}

public sealed record ConnectionScanResult(Guid ConnectionId, string ConnectionName, IReadOnlyList<CredentialRecord> Credentials, string? Error)
{
    public bool IsSuccess => Error is null;

    public static ConnectionScanResult Succeeded(Connection connection, IReadOnlyList<CredentialRecord> credentials) =>
        new(connection.Id, connection.Name, credentials, null);

    public static ConnectionScanResult Failed(Connection connection, string error) =>
        new(connection.Id, connection.Name, [], error);
}
