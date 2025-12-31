using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mediconnet_Backend.Core.Interfaces.Services;
using Microsoft.IdentityModel.Tokens;

namespace Mediconnet_Backend.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateTokenAsync(int userId, string role)
    {
        try
        {
            var jwtSecret = _configuration["Jwt:Secret"] ?? "default-super-secret-key-minimum-32-characters-long-!!!";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "MediConnect";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "MediConnectUsers";
            var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

            var key = Encoding.ASCII.GetBytes(jwtSecret);
            var userIdStr = userId.ToString();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userIdStr),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("userId", userIdStr),
                    new Claim("role", role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation($"JWT token generated for user {userId}");
            return await Task.FromResult(tokenString);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating JWT token: {ex.Message}");
            throw;
        }
    }

    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var jwtSecret = _configuration["Jwt:Secret"] ?? "default-super-secret-key-minimum-32-characters-long-!!!";
            var key = Encoding.ASCII.GetBytes(jwtSecret);

            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = jwtToken.Claims.First(x => x.Type == "userId").Value;

            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error extracting user ID from token: {ex.Message}");
            return null;
        }
    }
}
