using KeyLens.Entities;
using KeyLens.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace KeyLens.Tests;

/// <summary>
/// Unit tests for <see cref="ConnectionScanner"/>.
/// </summary>
public class ConnectionScannerTests
{
    private readonly Mock<IConnectionSecretStore> _secretStore = new();

    private static Connection AConnection(ConnectionType type = ConnectionType.Azure, string? secretName = "keylens-connection-abc") =>
        new(Guid.CreateVersion7())
        {
            OwnerId = Guid.NewGuid(),
            Name = "Production",
            Type = type,
            Config = "{}",
            SecretName = secretName,
        };

    private ConnectionScanner CreateSut(params ICredentialProviderFactory[] factories) =>
        new(factories, _secretStore.Object, NullLogger<ConnectionScanner>.Instance);

    /// <summary>
    /// Given a connection whose provider enumerates credentials
    /// When it is scanned
    /// Then the credentials are returned and the scan is reported as successful.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ScanAsync_ProviderSucceeds_ReturnsCredentials()
    {
        _secretStore.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("a-secret");

        var record = new CredentialRecord("Azure.KeyVault", "vault", "https://v", "secret1", CredentialKind.Secret, "secret1", null, null, true);
        var factory = new StubFactory(ConnectionType.Azure, [record]);

        var result = await CreateSut(factory).ScanAsync(AConnection(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Credentials);
        Assert.Null(result.Error);
    }

    /// <summary>
    /// Given a provider that throws while enumerating
    /// When the connection is scanned
    /// Then the failure is captured on the result rather than propagating, so one broken
    /// connection cannot fail the whole scan.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ScanAsync_ProviderThrows_CapturesErrorInsteadOfPropagating()
    {
        _secretStore.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("a-secret");

        var factory = new StubFactory(ConnectionType.Azure, throws: new InvalidOperationException("client secret expired"));

        var result = await CreateSut(factory).ScanAsync(AConnection(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("client secret expired", result.Error);
        Assert.Empty(result.Credentials);
    }

    /// <summary>
    /// Given the secret store cannot return a connection's secret
    /// When the connection is scanned
    /// Then the failure is reported against that connection.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ScanAsync_SecretUnavailable_ReportsFailure()
    {
        _secretStore.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("vault unreachable"));

        var result = await CreateSut(new StubFactory(ConnectionType.Azure, [])).ScanAsync(AConnection(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("vault unreachable", result.Error);
    }

    /// <summary>
    /// Given a connection type with no registered provider
    /// When it is scanned
    /// Then the result explains that nothing can scan it.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ScanAsync_NoProviderForType_ReportsFailure()
    {
        var result = await CreateSut(new StubFactory(ConnectionType.Azure, [])).ScanAsync(AConnection(ConnectionType.Aws), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Contains("Aws", result.Error);
    }

    /// <summary>
    /// Given a connection type that needs no secret
    /// When it is scanned
    /// Then the secret store is not consulted.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ScanAsync_ConnectionWithoutSecret_DoesNotConsultSecretStore()
    {
        var factory = new StubFactory(ConnectionType.TlsEndpoint, []);

        var result = await CreateSut(factory).ScanAsync(AConnection(ConnectionType.TlsEndpoint, secretName: null), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _secretStore.Verify(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class StubFactory(ConnectionType type, CredentialRecord[]? records = null, Exception? throws = null) : ICredentialProviderFactory
    {
        public ConnectionType Type => type;

        public IEnumerable<ICredentialProvider> Create(Connection connection, string? secret) =>
            [new StubProvider(records ?? [], throws)];
    }

    private sealed class StubProvider(CredentialRecord[] records, Exception? throws) : ICredentialProvider
    {
        public string Name => "Stub";

        public async IAsyncEnumerable<CredentialRecord> EnumerateAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (throws is not null) throw throws;

            foreach (var record in records)
            {
                yield return record;
            }

            await Task.CompletedTask;
        }

        public IEnumerable<string> RequiredPermissions() => [];
    }
}
