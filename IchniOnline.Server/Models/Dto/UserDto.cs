using IchniOnline.Server.Models;

namespace IchniOnline.Server.Models.Dto;

public record UserDto(
    Guid UserId,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    UserPermission Permission
);
