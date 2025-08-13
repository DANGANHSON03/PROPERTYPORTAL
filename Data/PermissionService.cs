using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace PropertyPortal.Data
{
    public interface IPermissionService
    {
        Task<IReadOnlyList<string>> GetPermissionsAsync(long userId);
        Task<(long userId, string email, int roleId, string passwordHash)?> GetUserByEmailAsync(string email);
    }

    /// <summary>
    /// Truy vấn dữ liệu phân quyền từ PostgreSQL (schema đã có view: user_permissions_v).
    /// Yêu cầu package: Dapper, Npgsql.
    /// </summary>
    public sealed class PermissionService : IPermissionService
    {
        private readonly string _connStr;

        public PermissionService(IConfiguration cfg)
        {
            _connStr = cfg.GetConnectionString("Default")
                       ?? throw new KeyNotFoundException("Missing ConnectionStrings:Default in appsettings.json");
        }

        /// <summary>
        /// Mở kết nối và trả về NpgsqlConnection để dùng await using (IAsyncDisposable)
        /// </summary>
        private async Task<NpgsqlConnection> OpenAsync()
        {
            var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            return conn;
        }

        /// <summary>
        /// Lấy danh sách permission (code) của user từ view user_permissions_v
        /// </summary>
        public async Task<IReadOnlyList<string>> GetPermissionsAsync(long userId)
        {
            const string sql = @"
                SELECT permission_code
                FROM user_permissions_v
                WHERE user_id = @userId;
            ";

            await using var conn = await OpenAsync();
            var rows = await conn.QueryAsync<string>(sql, new { userId });
            return rows.ToList();
        }

        /// <summary>
        /// Lấy thông tin user theo email để đăng nhập (id, email, role_id, password_hash)
        /// </summary>
        public async Task<(long userId, string email, int roleId, string passwordHash)?> GetUserByEmailAsync(string email)
        {
            const string sql = @"
                SELECT id AS userId,
                       email,
                       role_id AS roleId,
                       password_hash AS passwordHash
                FROM ""users""
                WHERE email = @email
                  AND is_active = TRUE
                LIMIT 1;
            ";

            await using var conn = await OpenAsync();
            var user = await conn.QueryFirstOrDefaultAsync<(long, string, int, string)?>(sql, new { email });
            return user; // null nếu không tìm thấy
        }
    }
}
