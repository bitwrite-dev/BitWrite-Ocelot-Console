using BitWrite.OcelotConsole.Application.Interfaces;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public class GetUserSessionsQueryHandler
{
    private readonly ISessionRepository _sessionRepository;

    public GetUserSessionsQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<GetUserSessionsResponse> HandleAsync(GetUserSessionsQuery query, CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        return new GetUserSessionsResponse(
            sessions
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new SessionInfo(
                    s.Id,
                    s.CreatedAt,
                    s.ExpiresAt,
                    s.LastAccessedAt,
                    s.IsRevoked))
                .ToList());
    }
}