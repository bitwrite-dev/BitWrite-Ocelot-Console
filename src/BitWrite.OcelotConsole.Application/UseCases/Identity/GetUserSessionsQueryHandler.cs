using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public class GetUserSessionsQueryHandler
{
    private readonly ISessionRepository _sessionRepository;

    public GetUserSessionsQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<IReadOnlyList<SessionInfo>> HandleAsync(GetUserSessionsQuery query, CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        return sessions.Select(s => new SessionInfo(
            s.Id,
            s.CreatedAt,
            s.ExpiresAt,
            s.LastAccessedAt,
            s.IsRevoked)).ToList();
    }
}