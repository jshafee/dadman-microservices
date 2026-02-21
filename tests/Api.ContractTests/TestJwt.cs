using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api.ContractTests;

internal static class TestJwt
{
    public const string Issuer = "https://tests.local";
    public const string Audience = "dadman-tests";
    public const string Key = "super-secret-test-signing-key-12345";

    public static string Create(params string[] scopes)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim("scope", string.Join(' ', scopes))
            ],
            notBefore: now,
            expires: now.AddMinutes(30),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
