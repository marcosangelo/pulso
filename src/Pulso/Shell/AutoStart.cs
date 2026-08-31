using Microsoft.Win32;

namespace Pulso.Shell;

internal static class AutoStart
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Pulso";

    public static string ExePath =>
        Environment.ProcessPath
        ?? System.IO.Path.Combine(AppContext.BaseDirectory, "Pulso.exe");

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, false);
            return key?.GetValue(ValueName) is string;
        }
        catch
        {
            return false;
        }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            if (enabled)
                key.SetValue(ValueName, $"\"{ExePath}\" --tray");
            else
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch
        {
            // sem permissão de registro — a caixa na UI desfaz sozinha no próximo load
        }
        Pulso.Theme.ThemeSettings.Update(s => s.StartWithWindows = enabled);
    }
}
