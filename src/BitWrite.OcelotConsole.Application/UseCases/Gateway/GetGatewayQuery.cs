using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Gateway;

public record GetGatewayQuery(
    GatewayId Id
);
