using System.Text.Json;
using System.Text.Json.Serialization;
using KeyLens.Entities;

namespace KeyLens.Connections;

public sealed record AzureConnectionConfig(Guid TenantId, Guid ClientId, IReadOnlyList<string>? SubscriptionIds = null);

public sealed record AwsConnectionConfig(string AccessKeyId, string Region, string? RoleArn = null);

public sealed record HashiCorpVaultConnectionConfig(Uri Address, string? Namespace = null);

public sealed record TlsEndpointConnectionConfig(IReadOnlyList<string> Hosts, int Port = 443);

public static class ConnectionConfig
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        /* Without this a config of the wrong shape binds to constructor defaults instead of
           failing, and the connection is only found to be unusable when it is next scanned. */
        RespectRequiredConstructorParameters = true,
    };

    public static string Serialise<T>(T config) => JsonSerializer.Serialize(config, Options);

    public static T Deserialise<T>(string config) =>
        JsonSerializer.Deserialize<T>(config, Options) ?? throw new InvalidOperationException($"Could not read {typeof(T).Name}.");

    public static object Deserialise(ConnectionType type, string config) => type switch
    {
        ConnectionType.Azure => Deserialise<AzureConnectionConfig>(config),
        ConnectionType.Aws => Deserialise<AwsConnectionConfig>(config),
        ConnectionType.HashiCorpVault => Deserialise<HashiCorpVaultConnectionConfig>(config),
        ConnectionType.TlsEndpoint => Deserialise<TlsEndpointConnectionConfig>(config),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown connection type."),
    };

    /// <summary>
    /// Whether a connection of this type carries a secret. TLS endpoints are reachable anonymously,
    /// so requiring one would make them unusable.
    /// </summary>
    public static bool RequiresSecret(ConnectionType type) => type != ConnectionType.TlsEndpoint;
}
