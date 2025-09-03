namespace KeyLens.Api.Handlers;

public static class GetCredentialsHandler
{
    public static async Task<IResult> HandleAsync(
        IEnumerable<ICredentialProvider> credentialProviders,
        CancellationToken cancellationToken = default)
    {
        var results = new List<CredentialRecord>();

        var tasks = credentialProviders.Select(async provider =>
        {
            var providerResults = new List<CredentialRecord>();
            await foreach (var credential in provider.EnumerateAsync(cancellationToken))
            {
                providerResults.Add(credential);
            }
            return providerResults;
        });

        var allProviderResults = await Task.WhenAll(tasks);

        foreach (var providerResults in allProviderResults)
        {
            results.AddRange(providerResults);
        }

        return Results.Ok(results.OrderBy(r => r.ExpiresOn.HasValue ? 0 : 1)
                                 .ThenBy(r => r.ExpiresOn));
    }
}
