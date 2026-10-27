using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace BakeryPos.Web;

public static class NetworkAddress
{
    /// <summary>The till's address on the shop network, for example 192.168.1.20, or null if none is found.</summary>
    public static string? FindLanAddress()
    {
        try
        {
            var candidates = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                            && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .Select(n => n.GetIPProperties())
                .OrderByDescending(p => p.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork
                                                                    && !g.Address.Equals(IPAddress.Any)))
                .SelectMany(p => p.UnicastAddresses)
                .Select(u => u.Address)
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IsLinkLocal(a));

            return candidates.FirstOrDefault()?.ToString();
        }
        catch (NetworkInformationException)
        {
            return null;
        }
    }

    private static bool IsLinkLocal(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] == 169 && bytes[1] == 254;
    }
}
