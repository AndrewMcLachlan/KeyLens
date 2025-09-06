using System.Runtime.CompilerServices;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Resources;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace KeyLens.Azure.KeyVault;

public class AzureKeyVaultDiscoveryCredentialProvider(ArmClient armClient, TokenCredential tokenCredential, ILogger<AzureKeyVaultDiscoveryCredentialProvider> logger) : ICredentialProvider
{
    public string Name => "Azure.KeyVault.Discovery";

    public async IAsyncEnumerable<CredentialRecord> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var allVaults = new List<(KeyVaultResource vault, SubscriptionData subscription)>();

        // First, collect all vaults from all subscriptions
        await foreach (var subscription in armClient.GetSubscriptions().GetAllAsync(cancellationToken))
        {
            await foreach (var keyVault in subscription.GetKeyVaultsAsync(cancellationToken: cancellationToken))
            {
                allVaults.Add((keyVault, subscription.Data));
            }
        }

        logger.LogInformation("Discovered {count} vaults", allVaults.Count);

        // Process all vaults in parallel
        var vaultTasks = allVaults.Select(async vaultInfo =>
        {
            var (keyVault, subscription) = vaultInfo;
            var vaultUri = keyVault.Data.Properties?.VaultUri;
            if (vaultUri == null) return [];

            // Process all credential types in parallel for each vault
            var tasks = new[]
            {
                EnumerateVaultCertificatesAsync(new CertificateClient(vaultUri, tokenCredential), keyVault.Data.Name, subscription, keyVault.Id, cancellationToken).ToListAsync(cancellationToken).AsTask(),
                EnumerateVaultKeysAsync(new KeyClient(vaultUri, tokenCredential), keyVault.Data.Name, subscription, keyVault.Id, cancellationToken).ToListAsync(cancellationToken).AsTask(),
                EnumerateVaultSecretsAsync(new SecretClient(vaultUri, tokenCredential), keyVault.Data.Name, subscription, keyVault.Id, cancellationToken).ToListAsync(cancellationToken).AsTask()
            };

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(x => x).ToArray();
        });

        var allResults = await Task.WhenAll(vaultTasks);

        // Yield all results in sorted order
        foreach (var credential in allResults.SelectMany(x => x).OrderBy(r => r.Container).ThenBy(r => r.CredentialId))
        {
            yield return credential;
        }
    }

    private static async IAsyncEnumerable<CredentialRecord> EnumerateVaultCertificatesAsync(
        CertificateClient certificateClient,
        string vaultName,
        SubscriptionData subscription,
        string vaultResourceId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<CredentialRecord> results = [];

        try
        {
            var pages = certificateClient.GetPropertiesOfCertificatesAsync(true, cancellationToken);

            await foreach (var certificateProps in pages.WithCancellation(cancellationToken))
            {
                // Get all versions of this certificate
                await foreach (var version in certificateClient.GetPropertiesOfCertificateVersionsAsync(certificateProps.Name, cancellationToken))
                {
                    results.Add(new CredentialRecord(
                        Provider: "Azure.KeyVault",
                        Container: $"{subscription.DisplayName}/{vaultName}",
                        ContainerId: certificateClient.VaultUri.ToString(),
                        CredentialId: version.Id.ToString(),
                        Kind: CredentialKind.Certificate,
                        Name: version.Name,
                        NotBefore: version.NotBefore,
                        ExpiresOn: version.ExpiresOn,
                        Enabled: version.Enabled.GetValueOrDefault(),
                        CredentialUri: new($"https://portal.azure.com/#@{subscription.TenantId}/resource{vaultResourceId}/certificates"),
                        Metadata: new
                        {
                            SubscriptionId = subscription.Id,
                            subscription.TenantId,
                            version.Tags,
                            version.X509ThumbprintString,
                            version.RecoveryLevel,
                        }
                    ));
                }
            }
        }
        catch (Exception)
        {
            // Skip vaults we don't have access to
            yield break;
        }

        foreach (var record in results.OrderBy(r => r.Container).ThenBy(r => r.CredentialId))
        {
            yield return record;
        }
    }

    private static async IAsyncEnumerable<CredentialRecord> EnumerateVaultKeysAsync(
        KeyClient keyClient,
        string vaultName,
        SubscriptionData subscription,
        string vaultResourceId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<CredentialRecord> results = [];

        try
        {
            var pages = keyClient.GetPropertiesOfKeysAsync(cancellationToken);

            await foreach (var keyProps in pages.WithCancellation(cancellationToken))
            {
                // Get all versions of this key
                await foreach (var version in keyClient.GetPropertiesOfKeyVersionsAsync(keyProps.Name, cancellationToken))
                {
                    results.Add(new CredentialRecord(
                        Provider: "Azure.KeyVault",
                        Container: $"{subscription.DisplayName}/{vaultName}",
                        ContainerId: keyClient.VaultUri.ToString(),
                        CredentialId: $"{version.Name}:{version.Version}",
                        Kind: CredentialKind.Key,
                        Name: version.Name,
                        NotBefore: version.NotBefore,
                        ExpiresOn: version.ExpiresOn,
                        Enabled: version.Enabled.GetValueOrDefault(),
                        CredentialUri: new($"https://portal.azure.com/#@{subscription.TenantId}/resource{vaultResourceId}/keys"),
                        Metadata: new
                        {
                            SubscriptionId = subscription.Id,
                            subscription.TenantId,
                            version.Tags,
                            version.RecoveryLevel,
                        }
                    ));
                }
            }
        }
        catch (Exception)
        {
            // Skip vaults we don't have access to
            yield break;
        }

        foreach (var record in results.OrderBy(r => r.Container).ThenBy(r => r.CredentialId))
        {
            yield return record;
        }
    }

    private static async IAsyncEnumerable<CredentialRecord> EnumerateVaultSecretsAsync(
        SecretClient secretClient,
        string vaultName,
        SubscriptionData subscription,
        string vaultResourceId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<CredentialRecord> results = [];

        try
        {
            var pages = secretClient.GetPropertiesOfSecretsAsync(cancellationToken);

            await foreach (var secretProps in pages.WithCancellation(cancellationToken))
            {
                // Get all versions of this secret
                await foreach (var version in secretClient.GetPropertiesOfSecretVersionsAsync(secretProps.Name, cancellationToken))
                {
                    results.Add(new CredentialRecord(
                        Provider: "Azure.KeyVault",
                        Container: $"{subscription.DisplayName}/{vaultName}",
                        ContainerId: secretClient.VaultUri.ToString(),
                        CredentialId: $"{version.Name}:{version.Version}",
                        Kind: CredentialKind.Secret,
                        Name: version.Name,
                        NotBefore: version.NotBefore,
                        ExpiresOn: version.ExpiresOn,
                        Enabled: version.Enabled.GetValueOrDefault(),
                        CredentialUri: new($"https://portal.azure.com/#@{subscription.TenantId}/resource{vaultResourceId}/secrets"),
                        Metadata: new
                        {
                            SubscriptionId = subscription.Id,
                            subscription.TenantId,
                            version.Tags,
                            version.RecoveryLevel,
                        }
                    ));
                }
            }
        }
        catch (Exception)
        {
            // Skip vaults we don't have access to
            yield break;
        }

        foreach (var record in results.OrderBy(r => r.Container).ThenBy(r => r.CredentialId))
        {
            yield return record;
        }
    }

    public IEnumerable<string> RequiredPermissions() =>
    [
        "Microsoft.KeyVault/vaults/read",
        "Microsoft.KeyVault/vaults/secrets/read",
        "Microsoft.Resources/subscriptions/read",
        "Microsoft.Resources/subscriptions/resourcegroups/read"
    ];
}
