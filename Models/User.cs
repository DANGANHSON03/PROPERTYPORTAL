using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyPortal.Models
{
    [Table("users", Schema = "real_estate")]
    public class User
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("role_id")]
        public int RoleId { get; set; }

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [Column("email_verified_at")]
        public DateTime? EmailVerifiedAt { get; set; }

        [Column("phone_verified_at")]
        public DateTime? PhoneVerifiedAt { get; set; }

        [Column("about")]
        public string? About { get; set; }

        [Column("agency_name")]
        public string? AgencyName { get; set; }

        [Column("license_no")]
        public string? LicenseNo { get; set; }

        [Column("hide_email")]
        public bool HideEmail { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
