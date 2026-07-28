// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Net;
using System.Net.Sockets;

#pragma warning disable SA1025 // Code should not contain multiple whitespace in a row

namespace Squidex.Hosting.Ssrf;

public static class SsrfHelper
{
    public static bool IsPrivateOrReservedIp(IPAddress ip, HashSet<IPAddress>? blackList)
    {
        // Normalize IPv6 forms that actually route to an IPv4 destination back to their
        // embedded IPv4 address, so the IPv4 private/reserved checks below apply to them.
        // Without this, a literal like ::ffff:10.0.0.1 or 64:ff9b::a9fe:a9fe has
        // AddressFamily.InterNetworkV6, the IPv4 block is skipped, and the address is
        // treated as public even though the socket connects to the internal IPv4.
        ip = Normalize(ip);

        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();

            var isBlocked =
                (bytes[0] == 10) ||                                         // 10.0.0.0/8
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||    // 172.16.0.0/12
                (bytes[0] == 192 && bytes[1] == 168) ||                     // 192.168.0.0/16
                (bytes[0] == 169 && bytes[1] == 254) ||                     // link-local
                (bytes[0] == 0) ||                                          // 0.0.0.0/8
                (bytes[0] >= 224 && bytes[0] <= 239) ||                     // 224.0.0.0/4 multicast
                (bytes[0] >= 240);                                          // 240.0.0.0/4 reserved

            if (isBlocked)
            {
                return true;
            }
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = ip.GetAddressBytes();

            var isBlocked =
                ip.IsIPv6LinkLocal ||                                       // fe80::/10
                ip.IsIPv6SiteLocal ||                                       // fec0::/10 (deprecated)
                ip.IsIPv6Multicast ||                                       // ff00::/8
                ((bytes[0] & 0xfe) == 0xfc);                                // fc00::/7 - Unique local

            if (isBlocked)
            {
                return true;
            }
        }

        if (blackList is {  Count: > 0 })
        {
            foreach (var blocked in blackList)
            {
                // Compare in a family-agnostic way so a blocked IPv4 entry (e.g. 169.254.169.254)
                // also matches its IPv4-mapped IPv6 form and vice versa. HashSet.Contains relies on
                // IPAddress.Equals, which returns false when the address families differ.
                var normalized = blocked.IsIPv4MappedToIPv6 ? blocked.MapToIPv4() : blocked;

                if (normalized.Equals(ip))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IPAddress Normalize(IPAddress ip)
    {
        if (ip.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return ip;
        }

        // ::ffff:a.b.c.d (IPv4-mapped) and ::a.b.c.d (IPv4-compatible).
        if (ip.IsIPv4MappedToIPv6)
        {
            return ip.MapToIPv4();
        }

        var bytes = ip.GetAddressBytes();

        // NAT64 well-known prefix 64:ff9b::/96 - embedded IPv4 is the last 32 bits.
        if (bytes[0] == 0x00 && bytes[1] == 0x64 &&
            bytes[2] == 0xff && bytes[3] == 0x9b &&
            bytes[4] == 0x00 && bytes[5] == 0x00 &&
            bytes[6] == 0x00 && bytes[7] == 0x00 &&
            bytes[8] == 0x00 && bytes[9] == 0x00 &&
            bytes[10] == 0x00 && bytes[11] == 0x00)
        {
            return new IPAddress(new[] { bytes[12], bytes[13], bytes[14], bytes[15] });
        }

        // 6to4 2002::/16 - embedded IPv4 is bytes 2..5.
        if (bytes[0] == 0x20 && bytes[1] == 0x02)
        {
            return new IPAddress(new[] { bytes[2], bytes[3], bytes[4], bytes[5] });
        }

        // IPv4-compatible ::a.b.c.d (first 12 bytes zero, not ::/::1 which are handled elsewhere).
        var allZero = true;
        for (var i = 0; i < 12; i++)
        {
            if (bytes[i] != 0)
            {
                allZero = false;
                break;
            }
        }

        if (allZero && (bytes[12] != 0 || bytes[13] != 0 || bytes[14] != 0))
        {
            return new IPAddress(new[] { bytes[12], bytes[13], bytes[14], bytes[15] });
        }

        return ip;
    }
}
