using System.Reflection;
using Asm.AspNetCore.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace KeyLens.Modules.Credentials;

public class Module : IModule
{
    private static readonly Assembly _assembly = typeof(Module).Assembly;

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        new Endpoints.Credentials().MapGroup(endpoints).RequireAuthorization();

        return endpoints;
    }

    public IServiceCollection AddServices(IServiceCollection services)
    {
        services.AddQueryHandlers(_assembly);

        return services;
    }
}
