namespace KeyLens.Modules.Credentials.Models;

/// <summary>
/// Connections that could not be scanned are reported alongside the credentials rather than thrown,
/// so one broken connection cannot blank the whole dashboard.
/// </summary>
public record CredentialsResult
{
    public required IEnumerable<CredentialRecord> Credentials { get; set; }

    public required IEnumerable<ConnectionError> Errors { get; set; }
}

public record ConnectionError(Guid ConnectionId, string ConnectionName, string Error);
