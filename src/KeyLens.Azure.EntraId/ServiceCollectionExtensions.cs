using Azure.Core;
using Azure.Identity;
using KeyLens;
using KeyLens.Azure.EntraId;
using KeyLens.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntraIdCredentialProviders(this IServiceCollection services)
    {
        services.AddKeyedScoped<ICredentialProvider>(ServiceKeys.Web, (provider, key) =>
        {
            var options = provider.GetRequiredService<IOptions<OAuthOptions>>();
            var accessTokenProvider = provider.GetRequiredService<IAccessTokenProvider>();
            var logger = provider.GetRequiredService<ILogger<AzureEntraIdDiscoveryCredentialProvider>>();

            var accessToken = accessTokenProvider.GetAccessToken();

            var credential = new OnBehalfOfCredential(
                tenantId: options.Value.TenantId.ToString(),
                clientId: options.Value.Audience,
                clientSecret: options.Value.ClientSecret,
                userAssertion: accessToken,
                options: new());

            return new AzureEntraIdDiscoveryCredentialProvider(credential, logger);
        });

        services.AddKeyedSingleton<ICredentialProvider>(ServiceKeys.Universal, (provider, key) =>
        {
            var logger = provider.GetRequiredService<ILogger<AzureEntraIdDiscoveryCredentialProvider>>();
            return new AzureEntraIdDiscoveryCredentialProvider(new DefaultAzureCredential(), logger);
        });

        return services;
    }
}
