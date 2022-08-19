// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HostingServiceExtensions
    {
        public static IServiceCollection AddInitializer(this IServiceCollection services)
        {
            services.AddOptionValidation();

            return services.AddHostedService<InitializerHost>();
        }

        public static IServiceCollection AddInitializer(this IServiceCollection services, string name, Func<IServiceProvider, CancellationToken, Task> action, int order = 0)
        {
            return services.AddSingleton<IInitializable>(c =>
            {
                return ActivatorUtilities.CreateInstance<DelegateSerializer2>(c, action, name, order);
            });
        }

        public static IServiceCollection AddInitializer(this IServiceCollection services, string name, Action<IServiceProvider> action, int order = 0)
        {
            return services.AddInitializer(name, (services, ct) =>
            {
                action(services);
                return Task.CompletedTask;
            }, order);
        }

        public static IServiceCollection AddInitializer<T>(this IServiceCollection services, string name, Func<T, CancellationToken, Task> action, int order = 0) where T : class
        {
            return services.AddSingleton<IInitializable>(c =>
            {
                return ActivatorUtilities.CreateInstance<DelegateSerializer2<T>>(c, action, name, order);
            });
        }

        public static IServiceCollection AddInitializer<T>(this IServiceCollection services, string name, Action<T> action, int order = 0) where T : class
        {
            return services.AddInitializer<T>(name, (services, ct) =>
            {
                action(services);
                return Task.CompletedTask;
            }, order);
        }

        public static IServiceCollection AddBackgroundProcesses(this IServiceCollection services)
        {
            return services.AddHostedService<BackgroundHost>();
        }
    }
}
