using AssetForge.Api.Security;

namespace AssetForge.Api;

/// <summary>Refuses to start with an unsafe or invalid configuration.</summary>
public static class StartupChecks
{
    private static readonly string[] LocalHosts = ["localhost", "127.0.0.1"];

    public static void Run(IConfiguration config)
    {
        string? token = config[$"{SecurityOptions.Section}:ApiToken"];
        if (string.IsNullOrWhiteSpace(token) || token.Length < SecurityOptions.MinTokenLength)
            throw new InvalidOperationException(
                $"Security:ApiToken must be set (user-secrets) to a random value of at least {SecurityOptions.MinTokenLength} characters.");

        string? allowedHosts = config["AllowedHosts"];
        string[] hosts = (allowedHosts ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (hosts.Length == 0 || hosts.Any(h => !LocalHosts.Contains(h, StringComparer.OrdinalIgnoreCase)))
            throw new InvalidOperationException("AllowedHosts may only list localhost and 127.0.0.1 (spec §5).");
    }
}
