using System.IO;
using System.Text;

namespace Pulso.Link;

/// <summary>%LOCALAPPDATA%\Pulso\companion.log — handshake do celular, sem token completo.</summary>
internal static class CompanionLog
{
    public static string Path { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pulso",
        "companion.log");

    private static readonly object Gate = new();

    public static void Line(string message)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (dir is not null) Directory.CreateDirectory(dir);
            var row = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}";
            lock (Gate)
            {
                var info = new FileInfo(Path);
                if (info.Exists && info.Length > 512 * 1024)
                    File.WriteAllText(Path, row, Encoding.UTF8);
                else
                    File.AppendAllText(Path, row, Encoding.UTF8);
            }
        }
        catch
        {
            // log não pode derrubar o hub
        }
    }
}
