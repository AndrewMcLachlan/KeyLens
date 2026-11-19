using KeyLens.Entities;

namespace KeyLens.Infrastructure;

public class KeyLensContext : DomainDbContext, IReadOnlyDbContext
{
    public KeyLensContext(IPublisher publisher) : base(publisher)
    {
    }

    public KeyLensContext(DbContextOptions<KeyLensContext> options, IPublisher publisher) : base(options, publisher)
    {
    }

    public DbSet<Connection> Connections { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KeyLensContext).Assembly);
    }
}
