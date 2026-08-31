using System.IO;
using System.Text.Json;

namespace Pulso.Theme;

/// <summary>%LOCALAPPDATA%\Pulso\settings.json — tema, token do hub, autostart, relay.</summary>
public sealed class ThemeSettings
{
    public string Theme { get; set; } = nameof(ThemeKind.CyberpunkNeon);
    public string? PairToken { get; set; }
    public bool StartWithWindows { get; set; }
    /// <summary>null = usa o IP da droplet; "" = só LAN, sem Ocean.</summary>
    public string? RelayUrl { get; set; }

    public const string DefaultRelayUrl = "ws://157.245.241.87:8080";

    public string? EffectiveRelayUrl =>
        RelayUrl == "" ? null : (string.IsNullOrWhiteSpace(RelayUrl) ? DefaultRelayUrl : RelayUrl);

    private static string PathOnDisk =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulso", "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static ThemeSettings Load()
    {
        try
        {
            var path = PathOnDisk;
            if (!File.Exists(path)) return new ThemeSettings();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ThemeSettings>(json) ?? new ThemeSettings();
        }
        catch
        {
            return new ThemeSettings();
        }
    }

    public void Save()
    {
        try
        {
            var path = PathOnDisk;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOpts));
        }
        catch
        {
            // preferência não é fatal
        }
    }

    public static void Update(Action<ThemeSettings> mutate)
    {
        var s = Load();
        mutate(s);
        s.Save();
    }
}
