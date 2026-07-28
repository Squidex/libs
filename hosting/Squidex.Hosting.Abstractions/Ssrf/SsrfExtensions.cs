// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Squidex.Hosting.Ssrf;

public static class SsrfExtensions
{
    public static IHttpClientBuilder EnableSsrfProtection(this IHttpClientBuilder builder )
    {
        builder.Services.AddTransient<SsrfProtectionHandler>();

        builder.AddHttpMessageHandler<SsrfProtectionHandler>();
        builder.ConfigurePrimaryHttpMessageHandler(services =>
        {
            var options = services.GetService<IOptions<SsrfOptions>>()?.Value ?? new ();

            return new SocketsHttpHandler
            {
                ConnectCallback = options.EnableDnsRebindingProtection
                    ? CreateSecureConnectCallback(options)
                    : null,
                AllowAutoRedirect = options.AllowAutoRedirect,
            };
        });

        return builder;
    }

    private static Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>> CreateSecureConnectCallback(SsrfOptions options)
    {
        return async (context, cancellationToken) =>
        {
            var host = context.DnsEndPoint.Host;

            if (options.IsWhitelistedHost(host))
            {
                return await CreateSockedAsync(context.DnsEndPoint, cancellationToken);
            }

            // Re-validate DNS to prevent DNS rebinding attacks.
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);

            foreach (var address in addresses)
            {
                if (SsrfHelper.IsPrivateOrReservedIp(address, options.BlockedIpAddresses))
                {
                    throw new HttpRequestException($"Connection to private IP blocked: {address}");
                }
            }

            // Connect to the validated addresses directly instead of the hostname. Connecting by
            // hostname would trigger a separate DNS resolution that could return a different
            // (private) IP than the one just validated (TOCTOU / DNS rebinding).
            return await CreateSockedAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
        };
    }

    private static async Task<NetworkStream> CreateSockedAsync(EndPoint endPoint,
        CancellationToken ct)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(endPoint, ct);

            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static async Task<NetworkStream> CreateSockedAsync(IPAddress[] addresses, int port,
        CancellationToken ct)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(addresses, port, ct);

            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
