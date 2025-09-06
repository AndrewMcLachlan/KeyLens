using Azure.Core;
using Azure.Identity;
using KeyLens;
using KeyLens.Azure.EntraId;
using KeyLens.Options;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntraIdCredentialProviders(this IServiceCollection services, TokenCredential ? tokenCredential = null)
    {
        var credential = tokenCredential ?? new DefaultAzureCredential();

        services.AddScoped<ICredentialProvider>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<OAuthOptions>>();
            var accessTokenProvider = provider.GetRequiredService<IAccessTokenProvider>();

            var accessToken = accessTokenProvider.GetAccessToken();

            var credential = new OnBehalfOfCredential(
                tenantId: options.Value.TenantId.ToString(),
                clientId: options.Value.Audience,
                clientSecret: options.Value.ClientSecret,
                userAssertion: accessToken,
                options: new());

            return new AzureEntraIdDiscoveryCredentialProvider(credential);
        });

        return services;
    }
}
