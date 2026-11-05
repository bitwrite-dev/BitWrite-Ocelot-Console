using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public class GetUserQueryHandler
{
    private readonly IUserRepository _userRepository;

    public GetUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponse?> HandleAsync(GetUserQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetAsync(query.Id, cancellationToken);
        if (user == null)
            return null;

        return MapToResponse(user);
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse(
            user.Id.Value,
            user.Username,
            user.DisplayName,
            user.Email,
            user.Status.Value,
            user.UserType,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastAuthenticatedAt,
            user.RoleIds);
    }
}
