using System.IO;
using System.Text.Json;

namespace Pulso.Theme;

/// <summary>
/// Preferência de tema, persistida em %LOCALAPPDATA%\Pulso\settings.json —
/// mesma pasta que já guarda crash.log e history.db.
/// </summary>
public sealed class ThemeSettings
{
    public string Theme { get; set; } = nameof(ThemeKind.CyberpunkNeon);

    private static string PathOnDisk =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulso", "settings.json");

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
            // Settings corrompido ou ilegível não pode travar a abertura do app.
            return new ThemeSettings();
        }
    }

    public void Save()
    {
        try
        {
            var path = PathOnDisk;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this));
        }
        catch
        {
            // Falha ao salvar preferência não é fatal — só volta ao padrão na próxima abertura.
        }
    }
}
