using Azure.Identity;
using Azure.ResourceManager;
using KeyLens.Connections;
using KeyLens.Entities;
using Microsoft.Extensions.Logging;

namespace KeyLens.Azure.KeyVault;

public class AzureKeyVaultCredentialProviderFactory(ILogger<AzureKeyVaultDiscoveryCredentialProvider> logger) : ICredentialProviderFactory
{
    public ConnectionType Type => ConnectionType.Azure;

    public IEnumerable<ICredentialProvider> Create(Connection connection, string? secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var config = ConnectionConfig.Deserialise<AzureConnectionConfig>(connection.Config);

        var credential = new ClientSecretCredential(config.TenantId.ToString(), config.ClientId.ToString(), secret);

        yield return new AzureKeyVaultDiscoveryCredentialProvider(new ArmClient(credential), credential, logger);
    }
}
