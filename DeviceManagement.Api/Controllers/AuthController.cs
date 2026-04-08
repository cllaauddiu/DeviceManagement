using DeviceManagement.Api.Models;
using DeviceManagement.Api.Models.Auth;
using DeviceManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly AuthService _authService;

    public AuthController(UserService userService, AuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest req)
    {
        var existing = await _userService.GetByEmailAsync(req.Email);
        if (existing != null)
            return Conflict("Un cont cu acest email există deja.");

        var user = new User
        {
            Name = req.Name,
            Email = req.Email,
            PasswordHash = _authService.HashPassword(req.Password),
            Role = "User",
            Location = req.Location
        };

        await _userService.CreateAsync(user);

        var token = _authService.GenerateToken(user);
        return Ok(new LoginResponse(user.Id!, user.Email, user.Name, token));
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req)
    {
        var user = await _userService.GetByEmailAsync(req.Email);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash) ||
            !_authService.VerifyPassword(req.Password, user.PasswordHash))
            return Unauthorized("Email sau parolă incorectă.");

        var token = _authService.GenerateToken(user);
        return Ok(new LoginResponse(user.Id!, user.Email, user.Name, token));
    }
}
