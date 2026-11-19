using Asm.Domain;
using KeyLens.Connections;
using KeyLens.Entities;
using KeyLens.Modules.Connections.Commands;
using KeyLens.Security;
using Microsoft.AspNetCore.Http;

namespace KeyLens.Modules.Connections.Tests.Commands;

/// <summary>
/// Unit tests for <see cref="CreateHandler"/>.
/// </summary>
public class CreateTests
{
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Mock<IConnectionRepository> _repository = new();
    private readonly Mock<IConnectionSecretStore> _secretStore = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserDataProvider> _userDataProvider = new();

    public CreateTests()
    {
        _userDataProvider.SetupGet(u => u.CurrentUserId).Returns(OwnerId);
        _secretStore.Setup(s => s.SetAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Guid id, string _, DateTimeOffset? _, CancellationToken _) => $"keylens-connection-{id:n}");
    }

    private CreateHandler CreateSut() =>
        new(_repository.Object, _secretStore.Object, _userDataProvider.Object, _unitOfWork.Object);

    /// <summary>
    /// Given an Azure connection with a secret
    /// When the connection is created
    /// Then it is owned by the current user and the secret is stored outside the database.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_AzureConnection_StoresSecretAndAssignsOwner()
    {
        Entities.Connection? added = null;
        _repository.Setup(r => r.Add(It.IsAny<Entities.Connection>()))
                   .Callback<Entities.Connection>(c => added = c)
                   .Returns<Entities.Connection>(c => c);

        var config = new AzureConnectionConfig(Guid.NewGuid(), Guid.NewGuid());

        var result = await CreateSut().Handle(new Create("Production", Entities.ConnectionType.Azure, config, "a-secret"), TestContext.Current.CancellationToken);

        Assert.NotNull(added);
        Assert.Equal(OwnerId, added!.OwnerId);
        Assert.Equal("Production", result.Name);
        Assert.True(result.HasSecret);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Given a connection whose secret is stored under its own identifier
    /// When the connection is created
    /// Then the identifier passed to the secret store is the one persisted on the connection,
    /// so connections cannot collide on a single shared secret.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_AzureConnection_NamesSecretAfterItsOwnId()
    {
        Entities.Connection? added = null;
        _repository.Setup(r => r.Add(It.IsAny<Entities.Connection>()))
                   .Callback<Entities.Connection>(c => added = c)
                   .Returns<Entities.Connection>(c => c);

        var config = new AzureConnectionConfig(Guid.NewGuid(), Guid.NewGuid());

        await CreateSut().Handle(new Create("Production", Entities.ConnectionType.Azure, config, "a-secret"), TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, added!.Id);
        _secretStore.Verify(s => s.SetAsync(added.Id, "a-secret", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal($"keylens-connection-{added.Id:n}", added.SecretName);
    }

    /// <summary>
    /// Given a connection type that requires a secret
    /// When no secret is supplied
    /// Then the command is rejected and nothing is persisted.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_AzureConnectionWithoutSecret_IsRejected()
    {
        var config = new AzureConnectionConfig(Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
            await CreateSut().Handle(new Create("Production", Entities.ConnectionType.Azure, config, null), TestContext.Current.CancellationToken));

        _repository.Verify(r => r.Add(It.IsAny<Entities.Connection>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Given a TLS endpoint connection, which is reached anonymously
    /// When it is created without a secret
    /// Then it is accepted and no secret is stored.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_TlsEndpointWithoutSecret_IsAccepted()
    {
        _repository.Setup(r => r.Add(It.IsAny<Entities.Connection>())).Returns<Entities.Connection>(c => c);

        var config = new TlsEndpointConnectionConfig(["example.com"]);

        var result = await CreateSut().Handle(new Create("Public certs", Entities.ConnectionType.TlsEndpoint, config, null), TestContext.Current.CancellationToken);

        Assert.False(result.HasSecret);
        _secretStore.Verify(s => s.SetAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Given configuration that does not match the shape its connection type expects
    /// When the connection is created
    /// Then the command is rejected.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_MismatchedConfig_IsRejected()
    {
        var config = new TlsEndpointConnectionConfig(["example.com"]);

        await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
            await CreateSut().Handle(new Create("Wrong", Entities.ConnectionType.Azure, config, "a-secret"), TestContext.Current.CancellationToken));

        _repository.Verify(r => r.Add(It.IsAny<Entities.Connection>()), Times.Never);
    }
}
