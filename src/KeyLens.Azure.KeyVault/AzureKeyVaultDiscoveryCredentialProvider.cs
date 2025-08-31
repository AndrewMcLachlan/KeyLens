
using System.Runtime.CompilerServices;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Resources;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;

namespace KeyLens.Azure.KeyVault;

public class AzureKeyVaultDiscoveryCredentialProvider(ArmClient armClient, TokenCredential tokenCredential) : ICredentialProvider
{
    public string Name => "Azure.KeyVault.Discovery";

    public async IAsyncEnumerable<CredentialRecord> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Get all subscriptions the user has access to
        await foreach (var subscription in armClient.GetSubscriptions().GetAllAsync(cancellationToken))
        {
            // Get all Key Vaults in each subscription
            await foreach (var keyVault in subscription.GetKeyVaultsAsync(cancellationToken: cancellationToken))
            {
                // Skip vaults that don't have the necessary permissions or are disabled
                if (keyVault.Data.Properties?.EnabledForDeployment != true &&
                    keyVault.Data.Properties?.EnabledForTemplateDeployment != true &&
                    keyVault.Data.Properties?.EnabledForDiskEncryption != true)
                {
                    // Try to access anyway - some vaults might not have these flags but still be accessible
                }

                // Create a SecretClient for this specific vault
                var vaultUri = keyVault.Data.Properties?.VaultUri;
                if (vaultUri == null) continue;

                var certificateClient = new CertificateClient(vaultUri, tokenCredential);

                // Enumerate secrets from this vault
                await foreach (var credential in EnumerateVaultCertificatesAsync(certificateClient, keyVault.Data.Name, subscription.Data, cancellationToken))
                {
                    yield return credential;
                }

                var keyClient = new KeyClient(vaultUri, tokenCredential);

                // Enumerate secrets from this vault
                await foreach (var credential in EnumerateVaultKeysAsync(keyClient, keyVault.Data.Name, subscription.Data, cancellationToken))
                {
                    yield return credential;
                }

                var secretClient = new SecretClient(vaultUri, tokenCredential);

                // Enumerate secrets from this vault
                await foreach (var credential in EnumerateVaultSecretsAsync(secretClient, keyVault.Data.Name, subscription.Data, cancellationToken))
                {
                    yield return credential;
                }
            }
        }
    }

    private static async IAsyncEnumerable<CredentialRecord> EnumerateVaultCertificatesAsync(
        CertificateClient certificateClient,
        string vaultName,
        SubscriptionData subscription,
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
                        Metadata: new
                        {
                            SubscriptionId = subscription.Id,
                            subscription.TenantId,
                            version.Tags,
                            version.X509ThumbprintString,
                            version.RecoveryLevel
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
                        Metadata: new
                        {
                            SubscriptionId = subscription.Id,
                            subscription.TenantId,
                            version.Tags,
                            version.RecoveryLevel
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
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<CredentialRecord> results = [];

        AsyncPageable<SecretProperties>? pages;

        try
        {
            pages = secretClient.GetPropertiesOfSecretsAsync(cancellationToken);

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
                        Metadata: new
                        {
                            SubscriptionId = subscription.Id,
                            subscription.TenantId,

                            version.Tags,
                            version.RecoveryLevel
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
