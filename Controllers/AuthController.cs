// File: Controllers/AuthController.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyPortal.Common;
using PropertyPortal.Services;
using PropertyPortal.Models;

namespace PropertyPortal.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwt;
    private readonly IPermissionService _perm;

    public AuthController(IJwtTokenService jwt, IPermissionService perm)
    {
        _jwt  = jwt;
        _perm = perm;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TokenPairResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            return BadRequest(ApiResponse<object>.Fail("Thiếu email hoặc mật khẩu."));

        var u = await _perm.GetUserByEmailAsync(model.Email);
        if (u is null)
            return Unauthorized(ApiResponse<object>.Fail("Email hoặc mật khẩu không đúng."));

        var (userId, email, roleId, passwordHash) = u.Value;

        if (!PasswordHasher.Verify(model.Password, passwordHash))
            return Unauthorized(ApiResponse<object>.Fail("Email hoặc mật khẩu không đúng."));

        // Lấy quyền theo role hiện tại
        var permissions = await _perm.GetPermissionsAsync(userId);

        // Phát cặp token
        var accessToken  = _jwt.CreateAccessToken(userId, email, roleId, permissions);
        var refreshToken = _jwt.CreateRefreshToken(userId, email, roleId, permissions);

        var data = new TokenPairResponse { AccessToken = accessToken, RefreshToken = refreshToken };
        return Ok(ApiResponse<TokenPairResponse>.Ok(data, "Đăng nhập thành công"));
    }

    /// <summary>Làm mới token từ refresh token (stateless, không DB cho token store)</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TokenPairResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.RefreshToken))
            return BadRequest(ApiResponse<object>.Fail("refreshToken is required"));

        var principal = _jwt.ValidateRefreshToken(req.RefreshToken);
        if (principal is null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid or expired refresh token"));

        // Lấy claim an toàn (không Parse thẳng)
        var subStr  = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(subStr, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Missing subject claim"));

        var emailFromToken = principal.FindFirstValue(JwtRegisteredClaimNames.Email);

        // --- NẠP LẠI ROLE & PERMISSIONS TỪ DB (ƯU TIÊN) ---
        // Cần IPermissionService có GetUserByIdAsync; nếu chưa có, fallback lấy roleId từ claim.
        int roleId;
        var userFromDb = await _perm.GetUserByIdAsync(userId);
        if (userFromDb is not null)
        {
            (long _, string dbEmail, int dbRoleId, string _) = userFromDb.Value;
            roleId = dbRoleId;
            // nếu email thay đổi, ưu tiên email DB
            emailFromToken = string.IsNullOrWhiteSpace(dbEmail) ? emailFromToken : dbEmail;
        }
        else
        {
            // Fallback: lấy role_id từ claim (nếu service chưa có method GetUserByIdAsync)
            var roleStr = principal.FindFirstValue("role_id");
            if (!int.TryParse(roleStr, out roleId))
                return Unauthorized(ApiResponse<object>.Fail("Missing role_id claim"));
        }

        // Luôn lấy permissions mới nhất từ DB theo userId
        var permissions = await _perm.GetPermissionsAsync(userId);

        // Cấp token mới
        var newAccess  = _jwt.CreateAccessToken(userId, emailFromToken ?? string.Empty, roleId, permissions);
        var newRefresh = _jwt.CreateRefreshToken(userId, emailFromToken ?? string.Empty, roleId, permissions);

        var data = new TokenPairResponse { AccessToken = newAccess, RefreshToken = newRefresh };
        return Ok(ApiResponse<TokenPairResponse>.Ok(data, "Làm mới token thành công"));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var userId   = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var email    = User.FindFirstValue(JwtRegisteredClaimNames.Email);
        var roleId   = User.FindFirstValue("role_id");
        var roleName = User.FindFirstValue("role_name") ?? User.FindFirstValue(ClaimTypes.Role) ?? "unknown";
        var perms    = User.FindAll("permission").Select(c => c.Value).Distinct().ToArray();

        var data = new { userId, email, roleId, roleName, permissions = perms };
        return Ok(ApiResponse<object>.Ok(data, "Thông tin người dùng"));
    }
}
