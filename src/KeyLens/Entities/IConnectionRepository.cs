namespace KeyLens.Entities;

public interface IConnectionRepository : IWritableRepository<Connection, Guid>, IDeletableRepository<Connection, Guid>
{
    Task<Connection?> GetByOwnerAsync(Guid ownerId, Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Connection>> GetEnabledByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Connection>> GetAllEnabledAsync(CancellationToken cancellationToken = default);
}
