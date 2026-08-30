namespace Pulso.Link;

public static class PairingUri
{
    public const int Protocol = 1;
    public const int DefaultPort = 8742;

    public static string Build(string host, int port, string token) =>
        $"pulso://link?v={Protocol}&h={Uri.EscapeDataString(host)}&p={port}&t={Uri.EscapeDataString(token)}";

    public static string NewToken() => Convert.ToHexString(Guid.NewGuid().ToByteArray())[..16].ToLowerInvariant();
}
