using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Service;

public record DeleteServiceCommand(
    ServiceId Id,
    string InitiatedBy = "",
    string CorrelationId = ""
);
