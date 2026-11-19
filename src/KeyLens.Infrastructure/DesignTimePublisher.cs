namespace KeyLens.Infrastructure;

/// <summary>
/// Stands in for the domain event publisher when the EF tooling constructs a context. Migrations
/// never raise domain events, so nothing is dropped.
/// </summary>
public sealed class DesignTimePublisher : IPublisher
{
    public ValueTask PublishPreSave<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken = default) where TDomainEvent : IDomainEvent =>
        ValueTask.CompletedTask;

    public ValueTask PublishPostSave<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken = default) where TDomainEvent : IDomainEvent =>
        ValueTask.CompletedTask;
}
