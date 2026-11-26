namespace AssetForge.Api.Security;

public sealed class SecurityOptions
{
    public const string Section = "Security";
    public const int MinTokenLength = 32;

    /// <summary>Bearer token required on every /api request (spec §5). Set in user-secrets; never logged.</summary>
    public string? ApiToken { get; set; }
}
