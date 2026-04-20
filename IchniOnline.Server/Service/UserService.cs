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
using System.Text.Json;

namespace IchniOnline.Server.Service;

public class UserService(
    ISqlSugarClient db,
    IConnectionMultiplexer redis,
    IOptions<JwtOptions> jwtOptions,
    ILogger<UserService> logger)
    : IUserService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    private const string SessionKeyPrefix = "session_key:";
    private const int SessionKeyExpirationMinutes = 5;
    private const string UserIdCachePrefix = "user:id:";
    private const string UserNameCachePrefix = "user:name:";
    private const string NullUserCacheValue = "__NOT_FOUND__";
    private static readonly TimeSpan NotFoundUserTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan UserCacheTtl = TimeSpan.FromMinutes(1);

    public async Task<ErrorOr<string>> GetSessionKeyAsync()
    {
        var sessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var database = redis.GetDatabase();
        await database.StringSetAsync(
            $"{SessionKeyPrefix}{sessionKey}",
            sessionKey,
            TimeSpan.FromMinutes(SessionKeyExpirationMinutes));

        logger.LogInformation("Generated new session key");
        return sessionKey;
    }

    public async Task<ErrorOr<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var database = redis.GetDatabase();
        var storedKey = await database.StringGetAsync($"{SessionKeyPrefix}{request.SessionKey}");

        if (!storedKey.HasValue)
        {
            logger.LogWarning("Invalid or expired session key: {SessionKey}", request.SessionKey);
            return Error.Unauthorized("Invalid or expired session key");
        }

        var user = await GetUserByUsernameCachedAsync(request.Username);

        if (user is null)
        {
            logger.LogWarning("User not found: {Username}", request.Username);
            return Error.NotFound("User not found");
        }

        if (string.IsNullOrEmpty(user.PasswordHashed))
        {
            return Error.Failure("Password not set for this user");
        }

        var decryptedPassword = DecryptPassword(request.EncryptedPassword, request.SessionKey);

        if (!BCrypt.Net.BCrypt.Verify(decryptedPassword, user.PasswordHashed))
        {
            logger.LogWarning("Invalid password for user: {Username}", request.Username);
            return Error.Unauthorized("Invalid credentials");
        }

        await database.KeyDeleteAsync($"{SessionKeyPrefix}{request.SessionKey}");

        var token = GenerateJwtToken(user);
        var response = new LoginResponse(
            token,
            new UserResponse(
                user.UserId,
                user.Username,
                user.DisplayName,
                user.AvatarUrl,
                (int)user.Permission));

        logger.LogInformation("User logged in successfully: {Username}", user.Username);
        return response;
    }

    public async Task<ErrorOr<UserResponse>> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await GetUserByUsernameCachedAsync(request.Username);

        if (existingUser is not null)
        {
            return Error.Conflict("Username already exists");
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

        await db.Insertable(newUser).ExecuteCommandAsync();
        await CacheUserAsync(newUser);

        logger.LogInformation("New user registered: {Username}", request.Username);

        return new UserResponse(
            newUser.UserId,
            newUser.Username,
            newUser.DisplayName,
            newUser.AvatarUrl,
            (int)newUser.Permission);
    }

    public async Task<ErrorOr<UserDto>> GetUserByIdAsync(Guid userId)
    {
        var user = await GetUserByIdCachedAsync(userId);

        if (user is null)
        {
            return Error.NotFound("User not found");
        }

        return new UserDto(
            user.UserId,
            user.Username,
            user.DisplayName,
            user.AvatarUrl,
            user.Permission);
    }

    public async Task<ErrorOr<UserDto>> UpdateUserAsync(Guid userId, string? displayName, string? avatarUrl)
    {
        var user = await GetUserByIdCachedAsync(userId);

        if (user is null)
        {
            return Error.NotFound("User not found");
        }

        if (displayName is not null)
            user.DisplayName = displayName;
        if (avatarUrl is not null)
            user.AvatarUrl = avatarUrl;

        await db.Updateable(user).ExecuteCommandAsync();
        await CacheUserAsync(user);

        logger.LogInformation("User updated: {UserId}", userId);

        return new UserDto(
            user.UserId,
            user.Username,
            user.DisplayName,
            user.AvatarUrl,
            user.Permission);
    }

    public async Task<ErrorOr<bool>> DeleteUserAsync(Guid userId)
    {
        var user = await GetUserByIdCachedAsync(userId);

        if (user is null)
        {
            return Error.NotFound("User not found");
        }

        await db.Deleteable<GameUser>()
            .Where(u => u.UserId == userId)
            .ExecuteCommandAsync();

        await CacheNullUserAsync(GetUserByIdCacheKey(userId));
        await CacheNullUserAsync(GetUserNameCacheKey(user.Username));

        logger.LogInformation("User deleted: {UserId}", userId);
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

    private async Task<GameUser?> GetUserByIdCachedAsync(Guid userId)
    {
        var db1 = redis.GetDatabase();
        var cacheKey = GetUserByIdCacheKey(userId);
        var cached = await db1.StringGetAsync(cacheKey);

        if (cached.HasValue)
        {
            if (cached == NullUserCacheValue)
            {
                return null;
            }

            var cachedUser = JsonSerializer.Deserialize<GameUser>(cached.ToString());
            if (cachedUser is not null)
            {
                return cachedUser;
            }
        }

        var user = await db.Queryable<GameUser>()
            .Where(u => u.UserId == userId)
            .FirstAsync();

        if (user is null)
        {
            await CacheNullUserAsync(cacheKey);
            return null;
        }

        await CacheUserAsync(user);
        return user;
    }

    private async Task<GameUser?> GetUserByUsernameCachedAsync(string username)
    {
        var db1 = redis.GetDatabase();
        var cacheKey = GetUserNameCacheKey(username);
        var cached = await db1.StringGetAsync(cacheKey);

        if (cached.HasValue)
        {
            if (cached == NullUserCacheValue)
            {
                return null;
            }

            var cachedUser = JsonSerializer.Deserialize<GameUser>(cached.ToString());
            if (cachedUser is not null)
            {
                return cachedUser;
            }
        }

        var user = await db.Queryable<GameUser>()
            .Where(u => u.Username == username)
            .FirstAsync();

        if (user is null)
        {
            await CacheNullUserAsync(cacheKey);
            return null;
        }

        await CacheUserAsync(user);
        return user;
    }

    private async Task CacheUserAsync(GameUser user)
    {
        var database = redis.GetDatabase();
        var payload = JsonSerializer.Serialize(user);

        await database.StringSetAsync(GetUserByIdCacheKey(user.UserId), payload, UserCacheTtl);
        await database.StringSetAsync(GetUserNameCacheKey(user.Username), payload, UserCacheTtl);
    }

    private async Task CacheNullUserAsync(string cacheKey)
    {
        var database = redis.GetDatabase();
        await database.StringSetAsync(cacheKey, NullUserCacheValue, NotFoundUserTtl);
    }
    

    private static string GetUserByIdCacheKey(Guid userId) => $"{UserIdCachePrefix}{userId}";

    private static string GetUserNameCacheKey(string username) => $"{UserNameCachePrefix}{username}";
}
