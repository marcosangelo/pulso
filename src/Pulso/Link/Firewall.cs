using System.Diagnostics;
using Pulso.Hardware;

namespace Pulso.Link;

/// <summary>Inbound 8742. Sem regra, o Windows deixa localhost e barra o celular.</summary>
internal static class Firewall
{
    public static void TryAllowInbound(int port)
    {
        if (!Privileges.IsAdministrator()) return;
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments =
                    $"advfirewall firewall add rule name=\"Pulso {port}\" dir=in action=allow protocol=TCP localport={port} profile=any",
                CreateNoWindow = true,
                UseShellExecute = false,
            });
            proc?.WaitForExit(4000);
        }
        catch
        {
            // sem regra: o prompt do Windows ainda pode aparecer
        }
    }
}
