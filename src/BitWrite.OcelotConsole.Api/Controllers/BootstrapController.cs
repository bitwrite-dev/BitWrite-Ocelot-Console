using BitWrite.OcelotConsole.Application.UseCases.Identity.Bootstrap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitWrite.OcelotConsole.Api.Controllers;

/// <summary>
/// IAM bootstrap endpoint. Uses deployment-supplied secrets only; never accepts credentials in request.
/// </summary>
[ApiController]
[Route("api/v1/bootstrap")]
public class BootstrapController : BaseApiController
{
    private readonly BootstrapInitialAdminCommandHandler _handler;

    public BootstrapController(BootstrapInitialAdminCommandHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Triggers/completes initial administrator bootstrap using secure deployment configuration.
    /// </summary>
    [HttpPost("initial-admin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BootstrapInitialAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BootstrapInitialAdminResponse>> BootstrapInitialAdmin(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _handler.HandleAsync(new BootstrapInitialAdminCommand(InitiatedBy, CorrelationId), cancellationToken);
            if (!result.Completed && !result.Created)
                return Conflict(result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private string InitiatedBy =>
        User.Identity?.Name
        ?? User.Claims.FirstOrDefault(c => c.Type == "name")?.Value
        ?? User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
        ?? "bootstrap:anonymous";
}
