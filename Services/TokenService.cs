using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CampusLostAndFound.Models;
using Microsoft.IdentityModel.Tokens;
using SecurityClaim = System.Security.Claims.Claim;

namespace CampusLostAndFound.Services;

public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "CampusLostAndFound";
    public string Audience { get; set; } = "CampusLostAndFound";
    public int ExpiryHours { get; set; } = 8;
}

/// <summary>Issues signed JWTs for the REST API layer.</summary>
public class TokenService
{
    private readonly JwtOptions _options;

    public TokenService(JwtOptions options) => _options = options;

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new SecurityClaim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new SecurityClaim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new SecurityClaim(ClaimTypes.Name, user.Name),
            new SecurityClaim(ClaimTypes.Email, user.Email),
            new SecurityClaim(ClaimTypes.Role, user.Role),
            new SecurityClaim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_options.ExpiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
