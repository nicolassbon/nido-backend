using System.Net;
using System.Net.Sockets;
using Nido.Infrastructure.Email;

namespace Nido.Infrastructure.Tests.Email;

public sealed class SmtpConnectionHelperTests
{
    [Fact]
    public void SelectPreferredAddress_WithIPv4AndIPv6_ReturnsIPv4()
    {
        var ipv4 = IPAddress.Parse("192.168.1.1");
        var ipv6 = IPAddress.Parse("2001:db8::1");
        var addresses = new[] { ipv6, ipv4 };

        var result = SmtpConnectionHelper.SelectPreferredAddress(addresses);

        Assert.Equal(AddressFamily.InterNetwork, result.AddressFamily);
        Assert.Equal(ipv4, result);
    }

    [Fact]
    public void SelectPreferredAddress_OnlyIPv6_ReturnsFirstIPv6()
    {
        var ipv6a = IPAddress.Parse("2001:db8::1");
        var ipv6b = IPAddress.Parse("2001:db8::2");
        var addresses = new[] { ipv6a, ipv6b };

        var result = SmtpConnectionHelper.SelectPreferredAddress(addresses);

        Assert.Equal(ipv6a, result);
    }

    [Fact]
    public void SelectPreferredAddress_MultipleIPv4_ReturnsFirstIPv4()
    {
        var ipv4a = IPAddress.Parse("10.0.0.1");
        var ipv4b = IPAddress.Parse("10.0.0.2");
        var ipv6 = IPAddress.Parse("2001:db8::1");
        var addresses = new[] { ipv6, ipv4a, ipv4b };

        var result = SmtpConnectionHelper.SelectPreferredAddress(addresses);

        Assert.Equal(ipv4a, result);
    }

    [Fact]
    public void SelectPreferredAddress_EmptyArray_ThrowsInvalidOperationException()
    {
        var addresses = Array.Empty<IPAddress>();

        Assert.Throws<InvalidOperationException>(() =>
            SmtpConnectionHelper.SelectPreferredAddress(addresses));
    }
}
