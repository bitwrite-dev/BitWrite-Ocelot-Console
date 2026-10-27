using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using Microsoft.AspNetCore.Mvc;
using ApiDtos = BitWrite.OcelotConsole.Api.DTOs;
using AppPublication = BitWrite.OcelotConsole.Application.UseCases.Publication;

using DomainEnvironmentName = BitWrite.OcelotConsole.Domain.ValueObjects.Configuration.EnvironmentName;

namespace BitWrite.OcelotConsole.Api.Controllers;

[ApiController]
[Route("api/v1/publications")]
public class PublicationsController : BaseApiController
{
    private readonly AppPublication.ListPublicationsQueryHandler _listPublicationsQueryHandler;
    private readonly AppPublication.GetCurrentPublicationQueryHandler _getCurrentPublicationQueryHandler;
    private readonly AppPublication.GetPublicationHistoryQueryHandler _getPublicationHistoryQueryHandler;
    private readonly AppPublication.GetPublicationDetailQueryHandler _getPublicationDetailQueryHandler;
    private readonly AppPublication.CreatePublicationCommandHandler _createPublicationCommandHandler;
    private readonly AppPublication.ExecutePublicationCommandHandler _executePublicationCommandHandler;
    private readonly AppPublication.GetPublicationProgressQueryHandler _getPublicationProgressQueryHandler;
    private readonly AppPublication.GetPublicationDiffQueryHandler _getPublicationDiffQueryHandler;
    private readonly AppPublication.CancelPublicationCommandHandler _cancelPublicationCommandHandler;

    public PublicationsController(
        AppPublication.ListPublicationsQueryHandler listPublicationsQueryHandler,
        AppPublication.GetCurrentPublicationQueryHandler getCurrentPublicationQueryHandler,
        AppPublication.GetPublicationHistoryQueryHandler getPublicationHistoryQueryHandler,
        AppPublication.GetPublicationDetailQueryHandler getPublicationDetailQueryHandler,
        AppPublication.CreatePublicationCommandHandler createPublicationCommandHandler,
        AppPublication.ExecutePublicationCommandHandler executePublicationCommandHandler,
        AppPublication.GetPublicationProgressQueryHandler getPublicationProgressQueryHandler,
        AppPublication.GetPublicationDiffQueryHandler getPublicationDiffQueryHandler,
        AppPublication.CancelPublicationCommandHandler cancelPublicationCommandHandler)
    {
        _listPublicationsQueryHandler = listPublicationsQueryHandler;
        _getCurrentPublicationQueryHandler = getCurrentPublicationQueryHandler;
        _getPublicationHistoryQueryHandler = getPublicationHistoryQueryHandler;
        _getPublicationDetailQueryHandler = getPublicationDetailQueryHandler;
        _createPublicationCommandHandler = createPublicationCommandHandler;
        _executePublicationCommandHandler = executePublicationCommandHandler;
        _getPublicationProgressQueryHandler = getPublicationProgressQueryHandler;
        _getPublicationDiffQueryHandler = getPublicationDiffQueryHandler;
        _cancelPublicationCommandHandler = cancelPublicationCommandHandler;
    }

    [HttpGet]
    public async Task<ActionResult<ApiDtos.PublicationListResponse>> GetPublications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = new AppPublication.ListPublicationsQuery(page, pageSize);
            var result = await _listPublicationsQueryHandler.HandleAsync(query);

            var response = new ApiDtos.PublicationListResponse(
                result.Publications.Select(MapToResponse).ToList(),
                result.TotalCount,
                result.Page,
                result.PageSize
            );

            return HandleResult(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("current")]
    public async Task<ActionResult<ApiDtos.CurrentPublicationResponse>> GetCurrentPublication()
    {
        try
        {
            var query = new AppPublication.GetCurrentPublicationQuery();
            var result = await _getCurrentPublicationQueryHandler.HandleAsync(query);

            var response = new ApiDtos.CurrentPublicationResponse(
                result.Current != null ? MapToResponse(result.Current) : null,
                result.History.Select(MapToResponse).ToList()
            );

            return HandleResult(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<ApiDtos.PublicationListResponse>> GetPublicationHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = new AppPublication.GetPublicationHistoryQuery(page, pageSize);
            var result = await _getPublicationHistoryQueryHandler.HandleAsync(query);

            var response = new ApiDtos.PublicationListResponse(
                result.Publications.Select(MapToResponse).ToList(),
                result.TotalCount,
                result.Page,
                result.PageSize
            );

            return HandleResult(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiDtos.PublicationResponse>> GetPublicationDetail(string id)
    {
        try
        {
            var query = new AppPublication.GetPublicationDetailQuery(id);
            var result = await _getPublicationDetailQueryHandler.HandleAsync(query);

            if (result is null)
                return NotFound();

            return HandleResult(MapToResponse(result));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("create")]
    public async Task<ActionResult<ApiDtos.CreatePublicationResponse>> CreatePublication(
        [FromBody] ApiDtos.CreatePublicationRequest request)
    {
        try
        {
            var command = new AppPublication.CreatePublicationCommand(
                request.SnapshotVersion,
                request.InitiatedBy,
                DomainEnvironmentName.From(request.Environment),
                request.CorrelationId ?? string.Empty,
                request.TargetGatewayIds);

            var result = await _createPublicationCommandHandler.HandleAsync(command);

            var response = new ApiDtos.CreatePublicationResponse(
                result.PublicationId,
                result.SnapshotVersion,
                result.Status,
                result.GatewayProgress.Select(MapToGatewayProgress).ToList());

            return HandleResult(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("{id}/execute")]
    public async Task<ActionResult<ApiDtos.ExecutePublicationResponse>> ExecutePublication(
        string id,
        [FromBody] ApiDtos.ExecutePublicationRequest request)
    {
        try
        {
            var command = new AppPublication.ExecutePublicationCommand(
                id,
                request.InitiatedBy,
                DomainEnvironmentName.From(request.Environment ?? "development"),
                request.CorrelationId ?? string.Empty);

            var publicationId = await _executePublicationCommandHandler.HandleAsync(command);

            var response = new ApiDtos.ExecutePublicationResponse(publicationId);

            return HandleResult(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelPublication(
        string id,
        [FromBody] ApiDtos.CancelPublicationRequest request)
    {
        try
        {
            var command = new AppPublication.CancelPublicationCommand(
                id,
                request.InitiatedBy,
                request.CorrelationId ?? string.Empty);

            await _cancelPublicationCommandHandler.HandleAsync(command);

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
    [HttpGet("{id}/progress")]
    public async Task<ActionResult<ApiDtos.PublicationProgressResponse>> GetPublicationProgress(string id)
    {
        try
        {
            var query = new AppPublication.GetPublicationProgressQuery(id);
            var result = await _getPublicationProgressQueryHandler.HandleAsync(query);

            if (result == null)
                return NotFound();

            var response = new ApiDtos.PublicationProgressResponse(
                result.PublicationId,
                result.SnapshotVersion,
                result.Status,
                result.ProgressPercentage,
                result.GatewayProgress.Select(MapToGatewayProgress).ToList(),
                result.StartedAt,
                result.CompletedAt,
                result.FailureReason);

            return HandleResult(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("diff")]
    public async Task<ActionResult<ApiDtos.PublicationDiffResponse>> GetPublicationDiff(
        [FromQuery] string targetSnapshotVersion,
        [FromQuery] string? currentSnapshotVersion = null,
        [FromQuery] string? environment = null)
    {
        try
        {
            var query = new AppPublication.GetPublicationDiffQuery(
                targetSnapshotVersion,
                currentSnapshotVersion,
                string.IsNullOrEmpty(environment) ? null : DomainEnvironmentName.From(environment));

            var result = await _getPublicationDiffQueryHandler.HandleAsync(query);

            var response = new ApiDtos.PublicationDiffResponse(
                result.HasDifferences,
                result.Differences,
                result.TargetSnapshotVersion,
                result.CurrentSnapshotVersion);

            return HandleResult(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private static ApiDtos.PublicationResponse MapToResponse(AppPublication.PublicationResponse publication)
    {
        return new ApiDtos.PublicationResponse(
            publication.Id.Value.ToString(),
            publication.SnapshotVersion.Value,
            publication.Status.Value,
            publication.InitiatedBy,
            publication.StartedAt,
            publication.CompletedAt,
            publication.FailureReason,
            publication.GatewayStates.Select(MapToGatewayState).ToList()
        );
    }

    private static ApiDtos.GatewayDeploymentStateResponse MapToGatewayState(AppPublication.GatewayDeploymentStateResponse state)
    {
        return new ApiDtos.GatewayDeploymentStateResponse(
            state.GatewayId.Value.ToString(),
            state.Status,
            state.ReceivedAt,
            state.ValidatedAt,
            state.AppliedAt,
            state.HealthyAt,
            state.IsValid,
            state.FailedAt,
            state.FailureReason
        );
    }

    private static ApiDtos.GatewayProgressResponse MapToGatewayProgress(AppPublication.GatewayProgressResponse state)
    {
        return new ApiDtos.GatewayProgressResponse(
            state.GatewayId,
            state.GatewayName,
            state.Status,
            state.ReceivedAt,
            state.ValidatedAt,
            state.AppliedAt,
            state.HealthyAt,
            state.FailedAt,
            state.IsValid,
            state.FailureReason
        );
    }
}
