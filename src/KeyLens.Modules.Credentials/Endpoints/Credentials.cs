using Asm.AspNetCore;
using Asm.AspNetCore.Routing;
using KeyLens.Modules.Credentials.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Postie.AspNetCore;

namespace KeyLens.Modules.Credentials.Endpoints;

internal class Credentials : EndpointGroupBase
{
    public override string Path => "/credentials";

    public override string? Tag => "Credentials";

    protected override void MapEndpoints(IEndpointRouteBuilder builder)
    {
        builder.MapQuery<GetAll, Models.CredentialsResult>("/")
            .WithNames("Get Credentials");
    }
}
