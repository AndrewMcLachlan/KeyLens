using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KeyLens.Entities;

[AggregateRoot]
[Table("Connection")]
[PrimaryKey(nameof(Id))]
public class Connection : KeyedEntity<Guid>
{
    [SetsRequiredMembers]
    public Connection(Guid id) : base(id)
    {
        Name = null!;
        Config = null!;
    }

    public Connection() : base(default)
    {
    }

    public required Guid OwnerId { get; set; }

    [StringLength(100)]
    public required string Name { get; set; }

    [Column("ConnectionTypeId")]
    public required ConnectionType Type { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Provider-specific configuration as JSON. Opaque to the database: never queried, so it maps
    /// to a plain string and behaves identically on every provider.
    /// </summary>
    [StringLength(4000)]
    public required string Config { get; set; }

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset? LastScan { get; set; }

    [StringLength(1000)]
    public string? LastScanError { get; set; }

    /// <summary>
    /// Name of this connection's secret in the secret store. Null when the provider needs no secret.
    /// </summary>
    [StringLength(127)]
    public string? SecretName { get; set; }

    public DateTimeOffset? SecretUpdated { get; set; }

    public void Update(string name, bool enabled, string config)
    {
        Name = name;
        Enabled = enabled;
        Config = config;
    }

    public void RecordScanSuccess(DateTimeOffset at)
    {
        LastScan = at;
        LastScanError = null;
    }

    public void RecordScanFailure(DateTimeOffset at, string error)
    {
        LastScan = at;
        LastScanError = error.Length > 1000 ? error[..1000] : error;
    }
}
