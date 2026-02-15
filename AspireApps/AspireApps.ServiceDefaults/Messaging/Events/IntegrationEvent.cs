namespace AspireApps.ServiceDefaults.Messaging.Events;

public record IntegrationEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTime OncurredOn => DateTime.UtcNow;
    public string EventType => GetType().AssemblyQualifiedName;
}
