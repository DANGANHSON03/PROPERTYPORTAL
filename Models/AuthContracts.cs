namespace PropertyPortal.Models;

public class AuthContracts
{
public record LoginRequest(string Email, string Password);
public record TokenResponse(string AccessToken, DateTime ExpiresAtUtc, object User);
}
