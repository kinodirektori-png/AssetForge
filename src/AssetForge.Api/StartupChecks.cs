using AssetForge.Api.Security;

namespace AssetForge.Api;

/// <summary>Refuses to start with an unsafe or invalid configuration.</summary>
public static class StartupChecks
{
    public static void Run(IConfiguration config)
    {
        string? token = config[$"{SecurityOptions.Section}:ApiToken"];
        if (string.IsNullOrWhiteSpace(token) || token.Length < SecurityOptions.MinTokenLength)
            throw new InvalidOperationException(
                $"Security:ApiToken must be set (user-secrets) to a random value of at least {SecurityOptions.MinTokenLength} characters.");

        string? allowedHosts = config["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Split(';').Any(h => h.Trim() == "*"))
            throw new InvalidOperationException("AllowedHosts must list only local host names (spec §5).");
    }
}
