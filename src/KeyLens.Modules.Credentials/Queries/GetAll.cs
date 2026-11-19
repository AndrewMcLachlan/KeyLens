using KeyLens.Entities;

namespace KeyLens.Modules.Credentials.Queries;

public record GetAll() : IQuery<CredentialsResult>;

internal class GetAllHandler(
    IConnectionRepository repository,
    ConnectionScanner scanner,
    IUserDataProvider userDataProvider,
    IUnitOfWork unitOfWork) : IQueryHandler<GetAll, CredentialsResult>
{
    public async ValueTask<CredentialsResult> Handle(GetAll request, CancellationToken cancellationToken)
    {
        var connections = (await repository.GetEnabledByOwnerAsync(userDataProvider.CurrentUserId, cancellationToken)).ToList();

        var results = await Task.WhenAll(connections.Select(c => scanner.ScanAsync(c, cancellationToken)));

        var scannedAt = DateTimeOffset.UtcNow;

        foreach (var connection in connections)
        {
            var result = results.Single(r => r.ConnectionId == connection.Id);

            if (result.IsSuccess)
            {
                connection.RecordScanSuccess(scannedAt);
            }
            else
            {
                connection.RecordScanFailure(scannedAt, result.Error!);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new()
        {
            Credentials = results.SelectMany(r => r.Credentials)
                                 .DistinctBy(r => r.ToString())
                                 .OrderBy(r => r.ExpiresOn.HasValue ? 0 : 1)
                                 .ThenBy(r => r.ExpiresOn),
            Errors = results.Where(r => !r.IsSuccess).Select(r => new ConnectionError(r.ConnectionId, r.ConnectionName, r.Error!)),
        };
    }
}
