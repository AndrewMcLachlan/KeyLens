namespace KeyLens.Api.Handlers;

public static class GetCredentialsHandler
{
    public static async Task<IResult> HandleAsync(
        IEnumerable<ICredentialProvider> credentialProviders,
        CancellationToken cancellationToken = default)
    {
        var results = new List<CredentialRecord>();
        foreach (var provider in credentialProviders)
        {
            await foreach (var credential in provider.EnumerateAsync(cancellationToken))
            {
                results.Add(credential);
            }
        }
        return Results.Ok(results);
    }
}
