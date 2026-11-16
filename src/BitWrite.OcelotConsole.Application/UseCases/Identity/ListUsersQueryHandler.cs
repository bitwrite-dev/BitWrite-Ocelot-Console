using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public class ListUsersQueryHandler
{
    private readonly IUserRepository _userRepository;

    public ListUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ListUsersResponse> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return new ListUsersResponse(users.Select(MapToResponse).ToList());
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
