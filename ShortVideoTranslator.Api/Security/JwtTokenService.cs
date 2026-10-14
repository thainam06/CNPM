using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Security;

public class JwtTokenService(string secret)
{
    public SymmetricSecurityKey Key { get; } = new(Encoding.UTF8.GetBytes(secret));
    public const string Issuer = "SmartTourGuide";
    public const string Audience = "SmartTourClients";
    public string Create(PlatformUser user) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        issuer: Issuer, audience: Audience,
        claims: [new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Name, user.Username), new("stamp", user.SecurityStamp), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))],
        notBefore: DateTime.UtcNow, expires: DateTime.UtcNow.AddMinutes(30),
        signingCredentials: new(Key, SecurityAlgorithms.HmacSha256)));
}
