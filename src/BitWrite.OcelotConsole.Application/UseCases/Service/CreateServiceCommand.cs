using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Service;

public record CreateServiceCommand(
    string Name,
    string? Description = null,
    IReadOnlyList<DownstreamTarget>? DownstreamTargets = null,
    string InitiatedBy = "",
    string CorrelationId = ""
);
