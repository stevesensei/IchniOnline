using ErrorOr;
using IchniOnline.Server.Models.Requests;
using IchniOnline.Server.Models.Responses;
using IchniOnline.Server.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace IchniOnline.Server.Controller;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("session-key")]
    public async Task<GlobalResponse<string>> GetSessionKey()
    {
        var result = await _userService.GetSessionKeyAsync();

        return result.Match<GlobalResponse<string>>(
            sessionKey => GlobalResponse<string>.Ok(sessionKey, "Session key generated"),
            errors => GlobalResponse<string>.Unauthorized(errors.First().Description));
    }

    [HttpPost("login")]
    public async Task<GlobalResponse<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _userService.LoginAsync(request);

        return result.Match<GlobalResponse<LoginResponse>>(
            response => GlobalResponse<LoginResponse>.Ok(response, "Login successful"),
            errors => GlobalResponse<LoginResponse>.Unauthorized(errors.First().Description));
    }

    [HttpPost("register")]
    public async Task<GlobalResponse<UserResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _userService.RegisterAsync(request);

        return result.Match<GlobalResponse<UserResponse>>(
            response => GlobalResponse<UserResponse>.Ok(response, "Registration successful"),
            errors => GlobalResponse<UserResponse>.BadRequest(errors.First().Description));
    }
}
