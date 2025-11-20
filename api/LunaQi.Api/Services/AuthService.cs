using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LunaQi.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace LunaQi.Api.Services;

public class AuthService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _key;
    private readonly SigningCredentials _creds;

    public AuthService(IConfiguration cfg)
    {
        var secret = cfg["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret missing");
        if (Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("Jwt:Secret must be at least 32 bytes.");

        _issuer = cfg["Jwt:Issuer"] ?? "LunaQi";
        _audience = cfg["Jwt:Audience"] ?? "LunaQiClient";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
    }

    public string CreateAccessToken(User user)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var jwt = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(60).UtcDateTime,
            signingCredentials: _creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    // PBKDF2 hashing
    public string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);

        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToHexString(salt) + ":" + Convert.ToHexString(hash);
    }

    public bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;

        var salt = Convert.FromHexString(parts[0]);
        var expected = Convert.FromHexString(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}