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
    private readonly IPermissionService _permService;
    private readonly IJwtTokenService _jwt;

    public AuthController(IPermissionService permService, IJwtTokenService jwt)
    {
        _permService = permService;
        _jwt = jwt;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _permService.GetUserByEmailAsync(model.Email);
        if (user is null)
            return Unauthorized(ApiResult<object>.Fail("Email hoặc mật khẩu không đúng."));

        var (userId, email, roleId, passwordHash) = user.Value;
        if (!PasswordHasher.Verify(model.Password, passwordHash))
            return Unauthorized(ApiResult<object>.Fail("Email hoặc mật khẩu không đúng."));

        var perms = await _permService.GetPermissionsAsync(userId);
        var token = _jwt.CreateToken(userId, email, perms);

        return Ok(ApiResult<object>.Ok(new { token, permissions = perms }, "Đăng nhập thành công"));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email);
        var perms = User.Claims.Where(c => c.Type == "perm").Select(c => c.Value).ToList();

        return Ok(ApiResult<object>.Ok(new { userId = sub, email, permissions = perms }, "Thông tin người dùng"));
    }

[HttpGet("dev-hash")]
[AllowAnonymous]
[SkipApiResponse]
public IActionResult DevHash([FromQuery] string pwd)
    => Ok(new { hash = PasswordHasher.Hash(pwd) });


}
