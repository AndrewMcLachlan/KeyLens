
namespace KeyLens.Modules.Connections.Queries;

public record GetAll() : IQuery<IEnumerable<Models.Connection>>;

internal class GetAllHandler(IQueryable<Entities.Connection> connections, IUserDataProvider userDataProvider) : IQueryHandler<GetAll, IEnumerable<Models.Connection>>
{
    public async ValueTask<IEnumerable<Models.Connection>> Handle(GetAll request, CancellationToken cancellationToken)
    {
        var owned = await connections.Where(c => c.OwnerId == userDataProvider.CurrentUserId)
                                     .OrderBy(c => c.Name)
                                     .ToListAsync(cancellationToken);

        return owned.Select(c => c.ToModel());
    }
}
