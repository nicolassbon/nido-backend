using System.Net;
using System.Net.Sockets;

namespace Nido.Infrastructure.Email;

internal static class SmtpConnectionHelper
{
    public static IPAddress SelectPreferredAddress(IPAddress[] addresses)
    {
        if (addresses.Length == 0)
            throw new InvalidOperationException("No addresses provided.");

        var ipv4 = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork);
        return ipv4 ?? addresses[0];
    }
}
