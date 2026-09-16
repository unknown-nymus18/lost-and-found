using CampusLostAndFound.DTOs;
using CampusLostAndFound.Services;
using Microsoft.AspNetCore.Mvc;

namespace CampusLostAndFound.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly TokenService _tokens;

    public AuthController(AuthService auth, TokenService tokens)
    {
        _auth = auth;
        _tokens = tokens;
    }

    /// <summary>Register a new student account and return a JWT.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var result = await _auth.RegisterAsync(req.Name, req.Email, req.Password);
        if (!result.Succeeded) return BadRequest(new { error = result.Error });

        var u = result.User!;
        return Ok(new AuthResponse(u.Id, u.Name, u.Email, u.Role, _tokens.CreateToken(u)));
    }

    /// <summary>Log in and return a JWT for use as a Bearer token.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var result = await _auth.ValidateAsync(req.Email, req.Password);
        if (!result.Succeeded) return Unauthorized(new { error = result.Error });

        var u = result.User!;
        return Ok(new AuthResponse(u.Id, u.Name, u.Email, u.Role, _tokens.CreateToken(u)));
    }
}
