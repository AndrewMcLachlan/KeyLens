using Asm.AspNetCore;
using Asm.AspNetCore.Routing;
using KeyLens.Modules.Connections.Commands;
using KeyLens.Modules.Connections.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Postie.AspNetCore;

namespace KeyLens.Modules.Connections.Endpoints;

internal class Connections : EndpointGroupBase
{
    public override string Path => "/connections";

    public override string? Tag => "Connections";

    protected override void MapEndpoints(IEndpointRouteBuilder builder)
    {
        builder.MapQuery<GetAll, IEnumerable<Models.Connection>>("/")
            .WithNames("Get Connections");

        builder.MapQuery<Get, Models.Connection>("/{id}")
            .WithNames("Get Connection");

        builder.MapPostCreate<Create, Models.Connection>("/", "Get Connection".ToMachine(), c => c.Id, RequestBinding.Body)
            .WithNames("Create Connection");

        builder.MapPatchCommand<Update, Models.Connection>("/{id}", binding: RequestBinding.Parameters)
            .WithNames("Update Connection");

        builder.MapDeleteCommand<Delete>("/{id}")
            .WithNames("Delete Connection");
    }
}
