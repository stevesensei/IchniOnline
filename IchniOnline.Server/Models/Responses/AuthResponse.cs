namespace IchniOnline.Server.Models.Responses;

public record LoginResponse(
    string Token,
    UserResponse User
);

public record UserResponse(
    Guid UserId,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    int Permission
);

public record SessionKeyResponse(
    string SessionKey,
    DateTime ExpiresAt
);
