namespace KeyLens;

public sealed record CredentialRecord(
    string Provider,              // "KeyVault", "EntraApp", "EntraB2C"
    string Container,             // vault name or app display name
    string ContainerId,           // vault URI or app/sp objectId
    string CredentialId,          // secret name+version, passwordCredential keyId, etc.
    CredentialKind Kind,
    string? Name,                 // KV secret name, cert subject, etc.
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpiresOn,
    bool Enabled,
    Uri? CredentialUri = null, // link to portal or other UI
    dynamic? Metadata = null
);
