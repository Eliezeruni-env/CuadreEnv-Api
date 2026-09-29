namespace crud_onion.Authentication;

public sealed class ClerkOptions
{
    public string Authority { get; set; } = string.Empty;
    public string? JwtIssuer { get; set; }
    public string? SecretKey { get; set; }
    public string BackendApiBaseUrl { get; set; } = "https://api.clerk.com/v1/";
    public string? AccessClaim { get; set; }
    public string AccessClaimExpectedValue { get; set; } = "true";
    public bool RequireOrganization { get; set; } = true;
    public int ClockSkewSeconds { get; set; } = 60;
}
