namespace SailNora.Foundation.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "sail-nora";

    public string Audience { get; set; } = "sail-nora";

    public string SigningKey { get; set; } = "sail-nora-dev-signing-key-change-in-production-32bytes!!";

    public int AccessTokenMinutes { get; set; } = 120;

    public int RefreshTokenDays { get; set; } = 7;
}
