using KeyLens;
using KeyLens.Azure.KeyVault;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKeyVaultCredentialProviders(this IServiceCollection services) =>
        services.AddScoped<ICredentialProviderFactory, AzureKeyVaultCredentialProviderFactory>();
}
