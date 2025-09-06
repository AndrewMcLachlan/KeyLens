using Azure.Identity;
using Azure.ResourceManager;
using KeyLens;
using KeyLens.Azure.KeyVault;
using KeyLens.Options;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKeyVaultCredentialProviders(this IServiceCollection services)
    {
        services.AddAzureClients(configure =>
        {
            configure.AddArmClient(null);
        });

        services.AddScoped<ICredentialProvider, AzureKeyVaultDiscoveryCredentialProvider>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<OAuthOptions>>();
            var accessTokenProvider = provider.GetRequiredService<IAccessTokenProvider>();
            var logger = provider.GetRequiredService<ILogger<AzureKeyVaultDiscoveryCredentialProvider>>();

            var accessToken = accessTokenProvider.GetAccessToken();

            var credential = new OnBehalfOfCredential(
                tenantId: options.Value.TenantId.ToString(),
                clientId: options.Value.Audience,
                clientSecret: options.Value.ClientSecret,
                userAssertion: accessToken,
                options: new()
                {
                    Diagnostics =
                    {
                        IsLoggingContentEnabled = logger.IsEnabled(LogLevel.Debug),
                        IsLoggingEnabled = logger.IsEnabled(LogLevel.Debug),
                    },
                });

            var armClient = provider.GetRequiredService<ArmClient>();
            return new AzureKeyVaultDiscoveryCredentialProvider(armClient, credential, logger);
        });

        return services;
    }
}
