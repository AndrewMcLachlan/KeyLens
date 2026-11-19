using KeyLens.Entities;

namespace KeyLens.Infrastructure.EntityConfigurations;

internal class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    public void Configure(EntityTypeBuilder<Connection> builder)
    {
        builder.HasIndex(c => c.OwnerId);

        builder.HasIndex(c => new { c.OwnerId, c.Name }).IsUnique();
    }
}
