using KeyLens.Entities;

namespace KeyLens.Infrastructure.Repositories;

internal class ConnectionRepository(KeyLensContext context)
    : RepositoryDeleteBase<KeyLensContext, Connection, Guid>(context), IConnectionRepository
{
    public Task<Connection?> GetByOwnerAsync(Guid ownerId, Guid id, CancellationToken cancellationToken = default) =>
        Entities.SingleOrDefaultAsync(c => c.OwnerId == ownerId && c.Id == id, cancellationToken);

    public async Task<IEnumerable<Connection>> GetEnabledByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        await Entities.Where(c => c.OwnerId == ownerId && c.Enabled).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Connection>> GetAllEnabledAsync(CancellationToken cancellationToken = default) =>
        await Entities.Where(c => c.Enabled).ToListAsync(cancellationToken);

    public override void Delete(Guid id) => Context.Remove(new Connection(id));
}
