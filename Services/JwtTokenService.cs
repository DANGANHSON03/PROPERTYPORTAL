using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PropertyPortal.Common;

namespace PropertyPortal.Services
{

    public interface IJwtTokenService
    {
        string CreateAccessToken(long userId, string email, int roleId, IEnumerable<string> permissions);
        string CreateRefreshToken(long userId, string email, int roleId, IEnumerable<string> permissions);
        ClaimsPrincipal? ValidateRefreshToken(string token); // verify bằng RefreshSecret
    }

    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _cfg;
        public JwtTokenService(IConfiguration cfg) => _cfg = cfg;

        public string CreateAccessToken(long userId, string email, int roleId, IEnumerable<string> permissions)
        {
            var roleName = RoleHelper.ToName(roleId);

            var keyStr = _cfg["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = BuildCommonClaims(userId, email, roleId, roleName, permissions);

            var issuer = _cfg["Jwt:Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer");
            var audience = _cfg["Jwt:Audience"] ?? throw new InvalidOperationException("Missing Jwt:Audience");
            var hours = double.Parse(_cfg["Jwt:ExpiresHours"] ?? "1");

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(hours),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string CreateRefreshToken(long userId, string email, int roleId, IEnumerable<string> permissions)
        {
            var roleName = RoleHelper.ToName(roleId);

            var keyStr = _cfg["Jwt:RefreshSecret"] ?? throw new InvalidOperationException("Missing Jwt:RefreshSecret");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = BuildCommonClaims(userId, email, roleId, roleName, permissions);

            var issuer = _cfg["Jwt:Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer");
            var audience = _cfg["Jwt:Audience"] ?? throw new InvalidOperationException("Missing Jwt:Audience");
            var days = double.Parse(_cfg["Jwt:RefreshExpiresDays"] ?? "7");

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(days),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateRefreshToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyStr = _cfg["Jwt:RefreshSecret"] ?? throw new InvalidOperationException("Missing Jwt:RefreshSecret");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _cfg["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _cfg["Jwt:Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromSeconds(30)
                }, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        private static List<Claim> BuildCommonClaims(long userId, string email, int roleId, string roleName, IEnumerable<string> permissions)
        {
            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),

            new(ClaimTypes.Role, roleName),
            new("role_id", roleId.ToString()),
            new("role_name", roleName)
        };
            // nếu quyền nhiều, cân nhắc chỉ add những quyền cần cho FE
            claims.AddRange(permissions.Select(p => new Claim("permission", p)));
            return claims;
        }
    }
}
