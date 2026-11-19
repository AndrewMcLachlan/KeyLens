using KeyLens;
using KeyLens.Azure.EntraId;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntraIdCredentialProviders(this IServiceCollection services) =>
        services.AddScoped<ICredentialProviderFactory, AzureEntraIdCredentialProviderFactory>();
}
