using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyPortal.Common;
using PropertyPortal.Data;
using PropertyPortal.Models;

namespace PropertyPortal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwt;
    public AuthController(IJwtTokenService jwt) => _jwt = jwt;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginModel model, [FromServices] IPermissionService permService)
    {
        var user = await permService.GetUserByEmailAsync(model.Email);
        if (user is null)
            return Unauthorized(ApiResponse<object>.Fail("Email hoặc mật khẩu không đúng."));

        var (userId, email, roleId, passwordHash) = user.Value;
        if (!PasswordHasher.Verify(model.Password, passwordHash))
            return Unauthorized(ApiResponse<object>.Fail("Email hoặc mật khẩu không đúng."));

        var roleName = RoleHelper.ToName(roleId);
        var perms    = await permService.GetPermissionsAsync(userId);

        var token = _jwt.CreateToken(userId, email, roleId, roleName, perms);

        var data = new
        {
            token,
            roleId,
            role = roleName,
            permissions = perms
        };
        return Ok(ApiResponse<object>.Ok(data, "Đăng nhập thành công"));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var sub   = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email);
        var role  = User.FindFirstValue(ClaimTypes.Role) ?? "unknown";
        var roleIdStr = User.FindFirstValue("role_id");
        int.TryParse(roleIdStr, out var roleId);

        var perms = User.Claims.Where(c => c.Type == "perm").Select(c => c.Value).Distinct().ToArray();

        var data = new
        {
            userId = sub,
            email,
            roleId,
            role,
            permissions = perms
        };
        return Ok(ApiResponse<object>.Ok(data, "Thông tin người dùng"));
    }
}
