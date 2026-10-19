using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Gateway;

public record RegisterGatewayCommand(
    string Name,
    string? Description,
    string InitiatedBy,
    string CorrelationId = ""
);
