using KeyLens.Connections;

namespace KeyLens.Modules.Connections.Models;

public record Connection
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required Entities.ConnectionType Type { get; set; }

    public bool Enabled { get; set; }

    public required object Config { get; set; }

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset? LastScan { get; set; }

    public string? LastScanError { get; set; }

    public bool HasSecret { get; set; }
}

public static class ConnectionExtensions
{
    public static Connection ToModel(this Entities.Connection connection) =>
        new()
        {
            Id = connection.Id,
            Name = connection.Name,
            Type = connection.Type,
            Enabled = connection.Enabled,
            Config = ConnectionConfig.Deserialise(connection.Type, connection.Config),
            Created = connection.Created,
            LastScan = connection.LastScan,
            LastScanError = connection.LastScanError,
            HasSecret = connection.SecretName is not null,
        };
}
