namespace PropertyPortal.Models;

public class LoginModel
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}
public sealed class TokenPairResponse
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
}

public sealed class RefreshRequest
{
    public string RefreshToken { get; set; } = default!;
}