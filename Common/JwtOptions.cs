namespace PropertyPortal.Common;

public class JwtOptions
{
    public string Key { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int ExpiresHours { get; set; } = 8;
    public string RefreshSecret { get; set; } = default!;
}
