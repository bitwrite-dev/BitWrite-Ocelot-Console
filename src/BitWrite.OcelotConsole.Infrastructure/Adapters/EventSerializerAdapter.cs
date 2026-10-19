using BitWrite.OcelotConsole.Domain.Events;
using AppIEventSerializer = BitWrite.OcelotConsole.Application.Interfaces.IEventSerializer;
using InfraIEventSerializer = BitWrite.OcelotConsole.Infrastructure.Outbox.IEventSerializer;

namespace BitWrite.OcelotConsole.Infrastructure.Adapters;

/// <summary>
/// Adapter that implements Application.Interfaces.IEventSerializer using Infrastructure.Outbox.IEventSerializer.
/// </summary>
public class EventSerializerAdapter : AppIEventSerializer
{
    private readonly InfraIEventSerializer _infraSerializer;

    public EventSerializerAdapter(InfraIEventSerializer infraSerializer)
    {
        _infraSerializer = infraSerializer;
    }

    public string Serialize(DomainEvent domainEvent)
    {
        return _infraSerializer.Serialize(domainEvent);
    }

    public DomainEvent? Deserialize(string json, string eventType)
    {
        return _infraSerializer.Deserialize(json, eventType);
    }
}
