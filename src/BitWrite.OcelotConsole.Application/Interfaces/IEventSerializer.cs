using BitWrite.OcelotConsole.Domain.Events;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IEventSerializer
{
    string Serialize(DomainEvent domainEvent);
    DomainEvent? Deserialize(string json, string eventType);
}
