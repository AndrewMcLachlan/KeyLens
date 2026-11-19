using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using KeyLens.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KeyLens.Infrastructure.Security;

public sealed class KeyVaultConnectionSecretStore : IConnectionSecretStore
{
    private readonly Lazy<SecretClient> _client;
    private readonly IMemoryCache _cache;
    private readonly KeyVaultOptions _options;

    public KeyVaultConnectionSecretStore(IOptions<KeyVaultOptions> options, TokenCredential credential, IMemoryCache cache)
    {
        _options = options.Value;
        _cache = cache;
        _client = new(() => new SecretClient(VaultUri, credential));
    }

    private Uri VaultUri =>
        _options.VaultUri ??
        throw new InvalidOperationException($"{KeyVaultOptions.SectionName}:{nameof(KeyVaultOptions.VaultUri)} is not configured. Connections that carry a secret cannot be read or written until it is.");

    public async Task<string> SetAsync(Guid connectionId, string secret, DateTimeOffset? expiresOn = null, CancellationToken cancellationToken = default)
    {
        var name = $"keylens-connection-{connectionId:n}";

        var value = new KeyVaultSecret(name, secret);

        /* Mirroring the credential's own expiry onto the vault secret means KeyLens's Key Vault
           provider reports its own connection secrets as they approach expiry. */
        value.Properties.ExpiresOn = expiresOn;

        await _client.Value.SetSecretAsync(value, cancellationToken);

        _cache.Remove(CacheKey(name));

        return name;
    }

    public async Task<string> GetAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey(secretName), out string? cached) && cached is not null)
        {
            return cached;
        }

        var secret = await _client.Value.GetSecretAsync(secretName, cancellationToken: cancellationToken);

        _cache.Set(CacheKey(secretName), secret.Value.Value, _options.CacheDuration);

        return secret.Value.Value;
    }

    public async Task DeleteAsync(string secretName, CancellationToken cancellationToken = default)
    {
        _cache.Remove(CacheKey(secretName));

        try
        {
            await _client.Value.StartDeleteSecretAsync(secretName, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
        }
    }

    private static string CacheKey(string secretName) => $"connection-secret:{secretName}";
}
