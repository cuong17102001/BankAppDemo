namespace IdentityService.API.Entities;

public class Client
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AllowedScopes { get; set; } = string.Empty; // space-separated
    public string AllowedAudiences { get; set; } = string.Empty; // space-separated
    public string AllowedRoles { get; set; } = string.Empty; // optional
}
