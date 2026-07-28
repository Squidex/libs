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
        // Normalize IPv4-mapped IPv6 addresses (e.g. ::ffff:169.254.169.254) to their IPv4 form.
        // Otherwise the IPv4-only range checks below are skipped and a dual-mode socket still
        // connects to the real IPv4 destination, bypassing the protection entirely.
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

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
}
