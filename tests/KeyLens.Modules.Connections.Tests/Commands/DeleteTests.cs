using Asm;
using Asm.Domain;
using KeyLens.Entities;
using KeyLens.Modules.Connections.Commands;
using KeyLens.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace KeyLens.Modules.Connections.Tests.Commands;

/// <summary>
/// Unit tests for <see cref="DeleteHandler"/>.
/// </summary>
public class DeleteTests
{
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Mock<IConnectionRepository> _repository = new();
    private readonly Mock<IConnectionSecretStore> _secretStore = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserDataProvider> _userDataProvider = new();

    public DeleteTests() => _userDataProvider.SetupGet(u => u.CurrentUserId).Returns(OwnerId);

    private DeleteHandler CreateSut() =>
        new(_repository.Object, _secretStore.Object, _userDataProvider.Object, _unitOfWork.Object, NullLogger<DeleteHandler>.Instance);

    private static Entities.Connection AConnection(string? secretName) =>
        new(Guid.CreateVersion7())
        {
            OwnerId = OwnerId,
            Name = "Production",
            Type = Entities.ConnectionType.Azure,
            Config = "{}",
            SecretName = secretName,
        };

    /// <summary>
    /// Given a connection that carries a secret
    /// When it is deleted
    /// Then the row is removed before the secret, so a failure cannot leave a connection
    /// pointing at a secret that no longer exists.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_ConnectionWithSecret_RemovesRowThenSecret()
    {
        var connection = AConnection("keylens-connection-abc");
        _repository.Setup(r => r.GetByOwnerAsync(OwnerId, connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(connection);

        var order = new List<string>();
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Callback(() => order.Add("save")).ReturnsAsync(1);
        _secretStore.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Callback(() => order.Add("secret")).Returns(Task.CompletedTask);

        await CreateSut().Handle(new Delete(connection.Id), TestContext.Current.CancellationToken);

        Assert.Equal(["save", "secret"], order);
    }

    /// <summary>
    /// Given the secret store fails while removing a deleted connection's secret
    /// When the connection is deleted
    /// Then the deletion still succeeds, leaving the secret as recoverable clutter.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_SecretStoreFails_DeletionStillSucceeds()
    {
        var connection = AConnection("keylens-connection-abc");
        _repository.Setup(r => r.GetByOwnerAsync(OwnerId, connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _secretStore.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("vault unreachable"));

        await CreateSut().Handle(new Delete(connection.Id), TestContext.Current.CancellationToken);

        _repository.Verify(r => r.Delete(connection), Times.Once);
    }

    /// <summary>
    /// Given a connection belonging to another user, or one that does not exist
    /// When a delete is attempted
    /// Then it is reported as not found and nothing is removed.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_ConnectionNotOwned_IsNotFound()
    {
        _repository.Setup(r => r.GetByOwnerAsync(OwnerId, It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Entities.Connection?)null);

        await Assert.ThrowsAsync<NotFoundException>(async () => await CreateSut().Handle(new Delete(Guid.CreateVersion7()), TestContext.Current.CancellationToken));

        _repository.Verify(r => r.Delete(It.IsAny<Entities.Connection>()), Times.Never);
        _secretStore.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
