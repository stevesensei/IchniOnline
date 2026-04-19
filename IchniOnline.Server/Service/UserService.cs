using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ErrorOr;
using IchniOnline.Server.Entities;
using IchniOnline.Server.Models;
using IchniOnline.Server.Models.Dto;
using IchniOnline.Server.Models.Options;
using IchniOnline.Server.Models.Requests;
using IchniOnline.Server.Models.Responses;
using IchniOnline.Server.Service.Interface;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SqlSugar;
using StackExchange.Redis;

namespace IchniOnline.Server.Service;

public class UserService : IUserService
{
    private readonly ISqlSugarClient _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<UserService> _logger;

    private const string SessionKeyPrefix = "session_key:";
    private const int SessionKeyExpirationMinutes = 5;

    public UserService(
        ISqlSugarClient db,
        IConnectionMultiplexer redis,
        IOptions<JwtOptions> jwtOptions,
        ILogger<UserService> logger)
    {
        _db = db;
        _redis = redis;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<ErrorOr<string>> GetSessionKeyAsync()
    {
        var sessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var db = _redis.GetDatabase();
        await db.StringSetAsync(
            $"{SessionKeyPrefix}{sessionKey}",
            sessionKey,
            TimeSpan.FromMinutes(SessionKeyExpirationMinutes));

        _logger.LogInformation("Generated new session key");
        return sessionKey;
    }

    public async Task<ErrorOr<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var db = _redis.GetDatabase();
        var storedKey = await db.StringGetAsync($"{SessionKeyPrefix}{request.SessionKey}");

        if (!storedKey.HasValue)
        {
            _logger.LogWarning("Invalid or expired session key: {SessionKey}", request.SessionKey);
            return ErrorOr.Error.Unauthorized("Invalid or expired session key");
        }

        var user = await _db.Queryable<GameUser>()
            .Where(u => u.Username == request.Username)
            .FirstAsync();

        if (user is null)
        {
            _logger.LogWarning("User not found: {Username}", request.Username);
            return ErrorOr.Error.NotFound("User not found");
        }

        if (string.IsNullOrEmpty(user.PasswordHashed))
        {
            return ErrorOr.Error.Failure("Password not set for this user");
        }

        var decryptedPassword = DecryptPassword(request.EncryptedPassword, request.SessionKey);

        if (!BCrypt.Net.BCrypt.Verify(decryptedPassword, user.PasswordHashed))
        {
            _logger.LogWarning("Invalid password for user: {Username}", request.Username);
            return ErrorOr.Error.Unauthorized("Invalid credentials");
        }

        await db.KeyDeleteAsync($"{SessionKeyPrefix}{request.SessionKey}");

        var token = GenerateJwtToken(user);
        var response = new LoginResponse(
            token,
            new UserResponse(
                user.UserId,
                user.Username,
                user.DisplayName,
                user.AvatarUrl,
                (int)user.Permission));

        _logger.LogInformation("User logged in successfully: {Username}", user.Username);
        return response;
    }

    public async Task<ErrorOr<UserResponse>> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _db.Queryable<GameUser>()
            .Where(u => u.Username == request.Username)
            .FirstAsync();

        if (existingUser is not null)
        {
            return ErrorOr.Error.Conflict("Username already exists");
        }

        var passwordHashed = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var newUser = new GameUser
        {
            UserId = Guid.NewGuid(),
            Username = request.Username,
            DisplayName = request.DisplayName,
            PasswordHashed = passwordHashed,
            Permission = UserPermission.Player
        };

        await _db.Insertable(newUser).ExecuteCommandAsync();

        _logger.LogInformation("New user registered: {Username}", request.Username);

        return new UserResponse(
            newUser.UserId,
            newUser.Username,
            newUser.DisplayName,
            newUser.AvatarUrl,
            (int)newUser.Permission);
    }

    public async Task<ErrorOr<UserDto>> GetUserByIdAsync(Guid userId)
    {
        var user = await _db.Queryable<GameUser>()
            .Where(u => u.UserId == userId)
            .FirstAsync();

        if (user is null)
        {
            return ErrorOr.Error.NotFound("User not found");
        }

        return new UserDto(
            user.UserId,
            user.Username,
            user.DisplayName,
            user.AvatarUrl,
            (UserPermission)user.Permission);
    }

    public async Task<ErrorOr<UserDto>> UpdateUserAsync(Guid userId, string? displayName, string? avatarUrl)
    {
        var user = await _db.Queryable<GameUser>()
            .Where(u => u.UserId == userId)
            .FirstAsync();

        if (user is null)
        {
            return ErrorOr.Error.NotFound("User not found");
        }

        if (displayName is not null)
            user.DisplayName = displayName;
        if (avatarUrl is not null)
            user.AvatarUrl = avatarUrl;

        await _db.Updateable(user).ExecuteCommandAsync();

        _logger.LogInformation("User updated: {UserId}", userId);

        return new UserDto(
            user.UserId,
            user.Username,
            user.DisplayName,
            user.AvatarUrl,
            (UserPermission)user.Permission);
    }

    public async Task<ErrorOr<bool>> DeleteUserAsync(Guid userId)
    {
        var user = await _db.Queryable<GameUser>()
            .Where(u => u.UserId == userId)
            .FirstAsync();

        if (user is null)
        {
            return ErrorOr.Error.NotFound("User not found");
        }

        await _db.Deleteable<GameUser>()
            .Where(u => u.UserId == userId)
            .ExecuteCommandAsync();

        _logger.LogInformation("User deleted: {UserId}", userId);
        return true;
    }

    private static string DecryptPassword(string encryptedPassword, string sessionKey)
    {
        var combined = Convert.FromBase64String(encryptedPassword);
        var sessionBytes = Encoding.UTF8.GetBytes(sessionKey);
        var decrypted = new byte[combined.Length];

        for (int i = 0; i < combined.Length; i++)
        {
            decrypted[i] = (byte)(combined[i] ^ sessionBytes[i % sessionBytes.Length]);
        }

        return Encoding.UTF8.GetString(decrypted);
    }

    private string GenerateJwtToken(GameUser user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role, user.Permission.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
