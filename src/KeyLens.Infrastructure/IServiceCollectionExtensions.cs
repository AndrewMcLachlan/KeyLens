using Azure.Core;
using Azure.Identity;
using KeyLens;
using KeyLens.Entities;
using KeyLens.Infrastructure;
using KeyLens.Infrastructure.Repositories;
using KeyLens.Infrastructure.Security;
using KeyLens.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddKeyLensInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<KeyLensContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("KeyLens")));

        services.AddOptions<KeyVaultOptions>()
                .Bind(configuration.GetSection(KeyVaultOptions.SectionName));

        services.AddMemoryCache();

        services.TryAddSingleton<TokenCredential>(new DefaultAzureCredential());

        services.AddSingleton<IConnectionSecretStore, KeyVaultConnectionSecretStore>();

        services.AddScoped<IConnectionRepository, ConnectionRepository>();

        services.AddAggregateRoots<KeyLensContext>(typeof(Connection).Assembly);
        services.AddScoped<ConnectionScanner>();

        services.AddDomainEvents(typeof(KeyLensContext).Assembly);

        return services.AddUnitOfWork<KeyLensContext>();
    }
}
