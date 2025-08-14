using System.Data;
using Dapper;
using Npgsql;

namespace PropertyPortal.Repositories;

public sealed class AuthUser
{
    public long Id { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = default!;
    public bool IsActive { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public interface IAuthRepository
{
    Task<AuthUser?> FindByEmailAsync(string email, CancellationToken ct = default);
}

public sealed class AuthRepository : IAuthRepository
{
    private readonly string _connStr;

    public AuthRepository(IConfiguration cfg)
    {
        // Lấy chuỗi kết nối từ appsettings.json
        _connStr = cfg.GetConnectionString("DefaultConnection")
                   ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
    }

    public async Task<AuthUser?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync(ct);

        // 1) Lấy user + role (LOWER để so sánh email không phân biệt hoa thường)
        const string sqlUser = @"
SET search_path TO real_estate;

SELECT u.id,
       u.email,
       u.password_hash     AS ""PasswordHash"",
       u.role_id           AS ""RoleId"",
       r.name              AS ""RoleName"",
       u.is_active         AS ""IsActive""
FROM ""users"" u
JOIN roles r ON r.id = u.role_id
WHERE LOWER(u.email) = LOWER(@email)
LIMIT 1;
";

        var user = await conn.QueryFirstOrDefaultAsync<AuthUser>(
            new CommandDefinition(sqlUser, new { email }, cancellationToken: ct, flags: CommandFlags.None));

        if (user is null) return null;

        // 2) Lấy permission theo role (chỉ quyền đang active)
        const string sqlPerms = @"
SET search_path TO real_estate;

SELECT p.code
FROM role_permissions rp
JOIN permissions p ON p.id = rp.permission_id
WHERE rp.role_id = @roleId
  AND p.is_active = TRUE;
";

        var perms = await conn.QueryAsync<string>(
            new CommandDefinition(sqlPerms, new { roleId = user.RoleId }, cancellationToken: ct));

        user.Permissions = perms.ToList();
        return user;
    }
}