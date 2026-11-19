using KeyLens.Entities;

namespace KeyLens;

/// <summary>
/// Builds the credential providers for a stored connection. A single connection may yield several
/// providers: one Azure service principal reaches both Key Vault and Entra ID.
/// </summary>
public interface ICredentialProviderFactory
{
    ConnectionType Type { get; }

    IEnumerable<ICredentialProvider> Create(Connection connection, string? secret);
}
