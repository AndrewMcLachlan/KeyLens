namespace KeyLens.Security;

/// <summary>
/// Holds connection secrets outside the database. Implementations return an opaque name that the
/// connection record stores in place of the secret itself.
/// </summary>
public interface IConnectionSecretStore
{
    Task<string> SetAsync(Guid connectionId, string secret, DateTimeOffset? expiresOn = null, CancellationToken cancellationToken = default);

    Task<string> GetAsync(string secretName, CancellationToken cancellationToken = default);

    Task DeleteAsync(string secretName, CancellationToken cancellationToken = default);
}
