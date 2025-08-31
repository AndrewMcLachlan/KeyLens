namespace KeyLens;

public interface ICredentialProvider
{
    string Name { get; }
    IAsyncEnumerable<CredentialRecord> EnumerateAsync(CancellationToken ct = default);
    IEnumerable<string> RequiredPermissions(); // human-readable summary
}