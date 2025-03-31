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
        private readonly IServiceCollection services;

        public InterfaceRegistrator(Registrator registerRequired, Registrator registerOptional, IServiceCollection services)
        {
            this.registerRequired = registerRequired;
            this.registerOptional = registerOptional;
            this.services = services;

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

        public IServiceCollection Done()
        {
            return services;
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
        services.AddScoped(typeof(T), factory);

        return new InterfaceRegistrator<T>((t, f) => services.AddScoped(t, f), services.TryAddScoped, services);
    }

    public static InterfaceRegistrator<T> AddScopedAs<T>(this IServiceCollection services) where T : class
    {
        services.AddScoped<T, T>();

        return new InterfaceRegistrator<T>((t, f) => services.AddScoped(t, f), services.TryAddScoped, services);
    }

    public static InterfaceRegistrator<T> AddTransientAs<T>(this IServiceCollection services, Func<IServiceProvider, T> factory) where T : class
    {
        services.AddTransient(typeof(T), factory);

        return new InterfaceRegistrator<T>((t, f) => services.AddTransient(t, f), services.TryAddTransient, services);
    }

    public static InterfaceRegistrator<T> AddTransientAs<T>(this IServiceCollection services) where T : class
    {
        services.AddTransient<T, T>();

        return new InterfaceRegistrator<T>((t, f) => services.AddTransient(t, f), services.TryAddTransient, services);
    }

    public static InterfaceRegistrator<T> AddSingletonAs<T>(this IServiceCollection services, Func<IServiceProvider, T> factory) where T : class
    {
        services.AddSingleton(typeof(T), factory);

        return new InterfaceRegistrator<T>((t, f) => services.AddSingleton(t, f), services.TryAddSingleton, services);
    }

    public static InterfaceRegistrator<T> AddSingletonAs<T>(this IServiceCollection services) where T : class
    {
        services.AddSingleton<T, T>();

        return new InterfaceRegistrator<T>((t, f) => services.AddSingleton(t, f), services.TryAddSingleton, services);
    }

    public static IServiceCollection AddSingletonWrapper<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService
    {
        return services.AddWrapper<TService, TImplementation>(ServiceLifetime.Singleton);
    }

    public static IServiceCollection AddWrapper<TService, TImplementation>(this IServiceCollection services, ServiceLifetime lifetime) where TService : class where TImplementation : class, TService
    {
        var existing = services.FirstOrDefault(x => x.ServiceType == typeof(TService) && x.Lifetime == lifetime);

        // Build an inner factory to cache the reflection logic.
        var innerFactory = BuildFactory(existing);

        var factory = ActivatorUtilities.CreateFactory(typeof(TImplementation), [typeof(TService)]);

        services.Add(
            new ServiceDescriptor(
                typeof(TService),
                s =>
                {
                    var wrapped = (TService)innerFactory(s);

                    return factory(s, [wrapped]);
                },
                lifetime));

        return services;
    }

    public static IServiceCollection AddSingletonWrapper<TService>(this IServiceCollection services, Func<IServiceProvider, TService, TService> factory) where TService : class
    {
        return services.AddWrapper(factory, ServiceLifetime.Singleton);
    }

    public static IServiceCollection AddWrapper<TService>(this IServiceCollection services, Func<IServiceProvider, TService, TService> factory, ServiceLifetime lifetime)
        where TService : class
    {
        var existing = services.FirstOrDefault(x => x.ServiceType == typeof(TService) && x.Lifetime == lifetime);

        // Build an inner factory to cache the reflection logic.
        var innerFactory = BuildFactory(existing);

        services.Add(
            new ServiceDescriptor(
                typeof(TService),
                s =>
                {
                    var wrapped = (TService)innerFactory(s);

                    return factory(s, wrapped);
                },
                lifetime));

        return services;
    }

    private static Func<IServiceProvider, object> BuildFactory(ServiceDescriptor? descriptor)
    {
        if (descriptor?.ImplementationFactory != null)
        {
            return descriptor.ImplementationFactory;
        }

        var instance = descriptor?.ImplementationInstance;

        if (instance != null)
        {
            return _ => instance;
        }

        if (descriptor?.ImplementationType != null)
        {
            var factory = ActivatorUtilities.CreateFactory(descriptor.ImplementationType, []);

            return s => factory(s, null);
        }

        throw new InvalidOperationException("Cannot instantiate wrapped service.");
    }
}
