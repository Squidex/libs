// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Messaging.Internal;

internal static class Extensions
{
    public static async Task<Provider<T>> CreateAsync<T>(this IServiceProvider serviceProvider) where T : class
    {
        var provider = new Provider<T>(serviceProvider);

        await provider.StartAsync();

        return provider;
    }

    public static MessagingBuilder Configure<T>(this MessagingBuilder builder, Action<T>? configure) where T : class
    {
        if (configure != null)
        {
            builder.Services.ConfigureOptional(configure);
        }

        return builder;
    }

    public static MessagingBuilder AddHandler(this MessagingBuilder builder, IMessageHandler? handler)
    {
        if (handler != null)
        {
            builder.Services.AddSingleton(handler);
        }

        return builder;
    }

    public static MessagingBuilder AddOverride(this MessagingBuilder builder, Action<MessagingBuilder>? configure)
    {
        configure?.Invoke(builder);
        return builder;
    }
}

public sealed class Provider<T>(IServiceProvider serviceProvider) : IAsyncDisposable where T : class
{
    public T Sut => serviceProvider.GetRequiredService<T>();

    public async Task StartAsync()
    {
        foreach (var initializable in serviceProvider.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await initializable.InitializeAsync(default);
        }

        foreach (var process in serviceProvider.GetRequiredService<IEnumerable<IBackgroundProcess>>())
        {
            await process.StartAsync(default);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var process in serviceProvider.GetRequiredService<IEnumerable<IBackgroundProcess>>())
        {
            await process.StopAsync(default);
        }

        foreach (var initializable in serviceProvider.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await initializable.ReleaseAsync(default);
        }

        (serviceProvider as IDisposable)?.Dispose();
    }
}
