using ErrorOr;
using IchniOnline.Server.Models.Dto;
using IchniOnline.Server.Models.Requests;
using IchniOnline.Server.Models.Responses;

namespace IchniOnline.Server.Service.Interface;

public interface IUserService
{
    Task<ErrorOr<string>> GetSessionKeyAsync();
    Task<ErrorOr<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ErrorOr<UserResponse>> RegisterAsync(RegisterRequest request);
    Task<ErrorOr<UserDto>> GetUserByIdAsync(Guid userId);
    Task<ErrorOr<UserDto>> UpdateUserAsync(Guid userId, string? displayName, string? avatarUrl);
    Task<ErrorOr<bool>> DeleteUserAsync(Guid userId);
}
