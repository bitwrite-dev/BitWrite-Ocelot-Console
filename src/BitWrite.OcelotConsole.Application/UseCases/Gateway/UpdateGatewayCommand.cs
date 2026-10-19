using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Gateway;

public record UpdateGatewayCommand(
    GatewayId Id,
    string? Name,
    string? Description,
    string InitiatedBy,
    string CorrelationId = ""
);
