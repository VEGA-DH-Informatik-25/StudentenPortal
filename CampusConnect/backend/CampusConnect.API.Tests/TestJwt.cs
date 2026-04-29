using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CampusConnect.API.Tests;

internal static class TestJwt
{
    public const string Secret = "CampusConnect-Test-Secret-Key-For-Integration-Tests-Only";
    public const string Issuer = "CampusConnect.Tests";
    public const string Audience = "CampusConnect.Tests";

    public static string CreateToken(Guid userId, string role = "Student")
        => CreateToken(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, role)
            ]);

    public static string CreateTokenWithoutUserId(string role = "Student")
        => CreateToken(
            [
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, role)
            ]);

    private static string CreateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
