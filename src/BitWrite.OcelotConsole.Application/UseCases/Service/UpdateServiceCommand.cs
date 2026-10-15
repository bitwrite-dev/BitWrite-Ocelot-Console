using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Service;

public record UpdateServiceCommand(
    ServiceId Id,
    string? Name = null,
    string? Description = null,
    IReadOnlyList<DownstreamTarget>? DownstreamTargets = null,
    string InitiatedBy = "",
    string CorrelationId = ""
);
