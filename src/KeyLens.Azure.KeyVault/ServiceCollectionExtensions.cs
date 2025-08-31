using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using KeyLens;
using KeyLens.Azure.KeyVault;
using Microsoft.Extensions.Azure;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKeyVaultCredentialProviders(this IServiceCollection services, TokenCredential? tokenCredential = null)
    {
        var credential = tokenCredential ?? new DefaultAzureCredential();

        services.AddAzureClients(configure =>
        {
            configure.UseCredential(credential);

            configure.AddArmClient(null);
        });

        services.AddSingleton<ICredentialProvider, AzureKeyVaultDiscoveryCredentialProvider>(provider =>
        {
            var armClient = provider.GetRequiredService<ArmClient>();
            return new AzureKeyVaultDiscoveryCredentialProvider(armClient, credential);
        });

        return services;
    }
}
