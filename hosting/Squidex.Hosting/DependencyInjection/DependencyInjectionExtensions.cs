// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection.Extensions;
using Squidex.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public delegate void Registrator(Type serviceType, Func<IServiceProvider, object> implementationFactory);

    public sealed class InterfaceRegistrator<T> where T : notnull
    {
        private readonly Registrator registerRequired;
        private readonly Registrator registerOptional;

        public InterfaceRegistrator(Registrator registerRequired, Registrator registerOptional)
        {
            this.registerRequired = registerRequired;
            this.registerOptional = registerOptional;

            var interfaces = typeof(T).GetInterfaces();

            if (interfaces.Contains(typeof(IInitializable)))
            {
                registerRequired(typeof(IInitializable), c => c.GetRequiredService<T>());
            }

            if (interfaces.Contains(typeof(IBackgroundProcess)))
            {
                registerRequired(typeof(IBackgroundProcess), c => c.GetRequiredService<T>());
            }
        }

        public InterfaceRegistrator<T> AsSelf()
        {
            return this;
        }

        public InterfaceRegistrator<T> AsOptional<TInterface>()
        {
            return AsOptional(typeof(TInterface));
        }

        public InterfaceRegistrator<T> As<TInterface>()
        {
            return As(typeof(TInterface));
        }

        public InterfaceRegistrator<T> AsOptional(Type type)
        {
            if (type != typeof(T))
            {
                registerOptional(type, c => c.GetRequiredService<T>());
            }

            return this;
        }

        public InterfaceRegistrator<T> As(Type type)
        {
            if (type != typeof(T))
            {
                registerRequired(type, c => c.GetRequiredService<T>());
            }

            return this;
        }
    }

    public static InterfaceRegistrator<T> AddScopedAs<T>(this IServiceCollection services, Func<IServiceProvider, T> factory) where T : class
    {
        services.AddTransient(typeof(T), factory);

        return new InterfaceRegistrator<T>((t, f) => services.AddScoped(t, f), services.TryAddScoped);
    }

    public static InterfaceRegistrator<T> AddScopedAs<T>(this IServiceCollection services) where T : class
    {
        services.AddTransient<T, T>();

        return new InterfaceRegistrator<T>((t, f) => services.AddScoped(t, f), services.TryAddScoped);
    }

    public static InterfaceRegistrator<T> AddTransientAs<T>(this IServiceCollection services, Func<IServiceProvider, T> factory) where T : class
    {
        services.AddTransient(typeof(T), factory);

        return new InterfaceRegistrator<T>((t, f) => services.AddTransient(t, f), services.TryAddTransient);
    }

    public static InterfaceRegistrator<T> AddTransientAs<T>(this IServiceCollection services) where T : class
    {
        services.AddTransient<T, T>();

        return new InterfaceRegistrator<T>((t, f) => services.AddTransient(t, f), services.TryAddTransient);
    }

    public static InterfaceRegistrator<T> AddSingletonAs<T>(this IServiceCollection services, Func<IServiceProvider, T> factory) where T : class
    {
        services.AddSingleton(typeof(T), factory);

        return new InterfaceRegistrator<T>((t, f) => services.AddSingleton(t, f), services.TryAddSingleton);
    }

    public static InterfaceRegistrator<T> AddSingletonAs<T>(this IServiceCollection services) where T : class
    {
        services.AddSingleton<T, T>();

        return new InterfaceRegistrator<T>((t, f) => services.AddSingleton(t, f), services.TryAddSingleton);
    }
}
