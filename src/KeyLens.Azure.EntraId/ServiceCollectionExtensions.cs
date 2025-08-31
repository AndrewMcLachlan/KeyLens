using Azure.Core;
using Azure.Identity;
using KeyLens;
using KeyLens.Azure.EntraId;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntraIdCredentialProviders(this IServiceCollection services, TokenCredential? tokenCredential = null, string? tenantId = null)
    {
        var credential = tokenCredential ?? new DefaultAzureCredential();

        services.AddSingleton<ICredentialProvider>(provider =>
        {
            return new AzureEntraIdDiscoveryCredentialProvider(credential, tenantId);
        });

        return services;
    }
}
