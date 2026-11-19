
namespace KeyLens.Modules.Connections.Queries;

public record Get(Guid Id) : IQuery<Models.Connection>;

internal class GetHandler(IQueryable<Entities.Connection> connections, IUserDataProvider userDataProvider) : IQueryHandler<Get, Models.Connection>
{
    public async ValueTask<Models.Connection> Handle(Get query, CancellationToken cancellationToken)
    {
        var connection = await connections.SingleOrDefaultAsync(c => c.OwnerId == userDataProvider.CurrentUserId && c.Id == query.Id, cancellationToken) ??
            throw new NotFoundException($"Connection {query.Id} was not found.");

        return connection.ToModel();
    }
}
