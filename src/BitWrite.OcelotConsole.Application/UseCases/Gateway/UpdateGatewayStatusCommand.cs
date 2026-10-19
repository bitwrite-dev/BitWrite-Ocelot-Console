using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;

namespace BitWrite.OcelotConsole.Application.UseCases.Gateway;

public record UpdateGatewayStatusCommand(
    GatewayId Id,
    RuntimeStatus Status,
    string InitiatedBy,
    string CorrelationId = ""
);
