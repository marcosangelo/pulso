namespace Pulso.Link;

public static class PairingUri
{
    public const int Protocol = 1;
    public const int DefaultPort = 8742;

    /// <summary>
    /// QR v1: LAN em h/p. Se o relay estiver configurado, rh/rp/rs — o app tenta Wi‑Fi primeiro.
    /// </summary>
    public static string Build(
        string lanHost,
        int lanPort,
        string token,
        string? relayHost = null,
        int? relayPort = null,
        bool relaySecure = false)
    {
        var q = $"pulso://link?v={Protocol}&h={Uri.EscapeDataString(lanHost)}&p={lanPort}&t={Uri.EscapeDataString(token)}";
        if (!string.IsNullOrWhiteSpace(relayHost) && relayPort is > 0)
        {
            q += $"&rh={Uri.EscapeDataString(relayHost)}&rp={relayPort}&rs={(relaySecure ? "wss" : "ws")}";
        }
        return q;
    }

    public static string NewToken() => Convert.ToHexString(Guid.NewGuid().ToByteArray())[..16].ToLowerInvariant();
}
