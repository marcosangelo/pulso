using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Pulso.Link;

public static class LanAddresses
{
    public static IReadOnlyList<string> Ipv4()
    {
        var skip = new[] { "virtual", "vmware", "vbox", "hyper-v", "docker", "wsl", "loopback", "bluetooth" };
        var list = new List<string>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;
            if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback) continue;
            var name = $"{nic.Name} {nic.Description}";
            if (skip.Any(s => name.Contains(s, StringComparison.OrdinalIgnoreCase))) continue;
            foreach (var addr in nic.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                if (IPAddress.IsLoopback(addr.Address)) continue;
                list.Add(addr.Address.ToString());
            }
        }
        return list.Distinct()
            .OrderBy(Score)
            .ToList();
    }

    private static int Score(string ip)
    {
        if (ip.StartsWith("192.168.", StringComparison.Ordinal)) return 0;
        if (ip.StartsWith("10.", StringComparison.Ordinal)) return 1;
        if (ip.StartsWith("172.", StringComparison.Ordinal)) return 2;
        return 3;
    }
}
