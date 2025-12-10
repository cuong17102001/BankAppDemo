namespace IdentityService.API.Models;

public class JwtOptions
{
    public string Issuer { get; set; } = "https://localhost:7001";
    public string DefaultAudience { get; set; } = "identity-audience";
    public string SigningKey { get; set; } = "dev_signing_key_very_long_for_hmac"; // dev only
    public int AccessTokenMinutes { get; set; } = 60;
}
