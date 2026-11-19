using System.ComponentModel;
using KeyLens.Security;
using Microsoft.Extensions.Logging;

namespace KeyLens.Modules.Connections.Commands;

[DisplayName("DeleteConnection")]
public sealed record Delete(Guid Id) : ICommand;

internal class DeleteHandler(
    Entities.IConnectionRepository repository,
    IConnectionSecretStore secretStore,
    IUserDataProvider userDataProvider,
    IUnitOfWork unitOfWork,
    ILogger<DeleteHandler> logger) : ICommandHandler<Delete>
{
    public async ValueTask Handle(Delete command, CancellationToken cancellationToken)
    {
        var connection = await repository.GetByOwnerAsync(userDataProvider.CurrentUserId, command.Id, cancellationToken) ??
            throw new NotFoundException($"Connection {command.Id} was not found.");

        var secretName = connection.SecretName;

        repository.Delete(connection);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        /* The row goes first: an orphaned vault secret is recoverable clutter, whereas a connection
           pointing at a deleted secret is not. */
        if (secretName is not null)
        {
            try
            {
                await secretStore.DeleteAsync(secretName, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Deleted connection {ConnectionId} but could not remove its secret {SecretName}", command.Id, secretName);
            }
        }
    }
}
