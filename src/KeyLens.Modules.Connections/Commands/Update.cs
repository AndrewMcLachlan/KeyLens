using System.ComponentModel;
using KeyLens.Security;

namespace KeyLens.Modules.Connections.Commands;

[DisplayName("UpdateConnection")]
public sealed record Update(Guid Id, string Name, object Config, string? Secret, bool Enabled) : ICommand<Models.Connection>;

internal class UpdateHandler(
    Entities.IConnectionRepository repository,
    IConnectionSecretStore secretStore,
    IUserDataProvider userDataProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<Update, Models.Connection>
{
    public async ValueTask<Models.Connection> Handle(Update command, CancellationToken cancellationToken)
    {
        var connection = await repository.GetByOwnerAsync(userDataProvider.CurrentUserId, command.Id, cancellationToken) ??
            throw new NotFoundException($"Connection {command.Id} was not found.");

        var config = ConnectionCommandValidation.Validate(connection.Type, command.Config, command.Secret ?? connection.SecretName);

        connection.Update(command.Name, command.Enabled, config);

        if (!String.IsNullOrWhiteSpace(command.Secret))
        {
            connection.SecretName = await secretStore.SetAsync(connection.Id, command.Secret, cancellationToken: cancellationToken);
            connection.SecretUpdated = DateTimeOffset.UtcNow;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return connection.ToModel();
    }
}
