using System.ComponentModel.DataAnnotations;

namespace BitWrite.OcelotConsole.SDK.Models.Requests;

public record CreateGatewayRequest(
    [Required][MaxLength(200)] string Name,
    [MaxLength(1000)] string? Description = null
);

public record UpdateGatewayRequest(
    [MaxLength(200)] string? Name = null,
    [MaxLength(1000)] string? Description = null
);

public record UpdateGatewayStatusRequest(
    [Required] string Status
);
