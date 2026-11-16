namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public record ListUsersQuery();

public record ListUsersResponse(IReadOnlyList<UserResponse> Users);
