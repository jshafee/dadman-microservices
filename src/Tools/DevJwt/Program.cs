using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var argsMap = ParseArgs(args);

var issuer = GetRequired(argsMap, "issuer");
var audience = GetRequired(argsMap, "audience");
var key = GetRequired(argsMap, "key");
var subject = GetRequired(argsMap, "sub");
var scopes = GetRequired(argsMap, "scopes")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Where(scope => !string.IsNullOrWhiteSpace(scope))
    .ToArray();

var minutes = 60;
if (argsMap.TryGetValue("minutes", out var minutesValue) && !string.IsNullOrWhiteSpace(minutesValue))
{
    minutes = int.Parse(minutesValue);
}

var now = DateTime.UtcNow;
var claims = new List<Claim>
{
    new(JwtRegisteredClaimNames.Sub, subject),
    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
    new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
    new("scope", string.Join(' ', scopes))
};

var credentials = new SigningCredentials(
    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
    SecurityAlgorithms.HmacSha256);

var token = new JwtSecurityToken(
    issuer: issuer,
    audience: audience,
    claims: claims,
    notBefore: now,
    expires: now.AddMinutes(minutes),
    signingCredentials: credentials);

Console.Write(new JwtSecurityTokenHandler().WriteToken(token));

static Dictionary<string, string?> ParseArgs(string[] args)
{
    var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (!arg.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = arg[2..];
        string? value = null;

        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = args[++i];
        }

        map[key] = value;
    }

    return map;
}

static string GetRequired(IReadOnlyDictionary<string, string?> argsMap, string key)
{
    if (argsMap.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    throw new ArgumentException($"Missing required argument '--{key}'.");
}
