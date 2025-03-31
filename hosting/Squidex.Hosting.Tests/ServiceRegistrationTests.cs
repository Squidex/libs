// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Squidex.Hosting;

public class ServiceRegistrationTests
{
    private interface IInterface1
    {
    }

    private interface IInterface2
    {
    }

    private interface IWrapped
    {
        public IWrapped? Inner { get; }
    }

    private sealed class ServiceWrapped : IWrapped, IInterface1, IInterface2
    {
        public IWrapped? Inner => null;
    }

    private sealed class ServiceWrapper(ServiceRegistrationTests.IWrapped inner) : IWrapped
    {
        public IWrapped? Inner { get; } = inner;
    }

    [Fact]
    public void Should_resolve_wrapped_from_type()
    {
        var outer =
            new ServiceCollection()
                .AddSingleton<IWrapped, ServiceWrapped>()
                .AddSingletonWrapper<IWrapped, ServiceWrapper>()
                .BuildServiceProvider()
                .GetRequiredService<IWrapped>();

        AssertWrapper(outer);
    }

    [Fact]
    public void Should_resolve_wrapped_from_instance()
    {
        var outer =
            new ServiceCollection()
                .AddSingleton<IWrapped>(new ServiceWrapped())
                .AddSingletonWrapper<IWrapped, ServiceWrapper>()
                .BuildServiceProvider()
                .GetRequiredService<IWrapped>();

        AssertWrapper(outer);
    }

    [Fact]
    public void Should_resolve_wrapped_from_factory()
    {
        var outer =
            new ServiceCollection()
                .AddSingleton<IWrapped>(_ => new ServiceWrapped())
                .AddSingletonWrapper<IWrapped>((s, inner) => new ServiceWrapper(inner))
                .BuildServiceProvider()
                .GetRequiredService<IWrapped>();

        AssertWrapper(outer);
    }

    [Fact]
    public void Should_resolve_wrapped_from_type_using_factory_registration()
    {
        var outer =
            new ServiceCollection()
                .AddSingleton<IWrapped, ServiceWrapped>()
                .AddSingletonWrapper<IWrapped, ServiceWrapper>()
                .BuildServiceProvider()
                .GetRequiredService<IWrapped>();

        AssertWrapper(outer);
    }

    [Fact]
    public void Should_resolve_wrapped_from_instance_using_factory_registration()
    {
        var outer =
            new ServiceCollection()
                .AddSingleton<IWrapped>(new ServiceWrapped())
                .AddSingletonWrapper<IWrapped>((s, inner) => new ServiceWrapper(inner))
                .BuildServiceProvider()
                .GetRequiredService<IWrapped>();

        AssertWrapper(outer);
    }

    [Fact]
    public void Should_resolve_wrapped_from_factory_using_factory_registration()
    {
        var outer =
            new ServiceCollection()
                .AddSingleton<IWrapped>(_ => new ServiceWrapped())
                .AddSingletonWrapper<IWrapped>((s, inner) => new ServiceWrapper(inner))
                .BuildServiceProvider()
                .GetRequiredService<IWrapped>();

        AssertWrapper(outer);
    }

    private static void AssertWrapper(IWrapped outer)
    {
        Assert.IsType<ServiceWrapper>(outer);
        Assert.IsType<ServiceWrapped>(outer.Inner);
    }

    [Fact]
    public void Should_resolve_singleton_with_as_registration()
    {
        var services =
            new ServiceCollection()
                .AddSingletonAs<ServiceWrapped>()
                    .As<IInterface1>()
                    .As<IInterface2>()
                    .AsSelf()
                    .Done()
                .BuildServiceProvider();

        AssetService(services, true);
    }

    [Fact]
    public void Should_resolve_singleton_with_as_registration_using_factory_registration()
    {
        var services =
            new ServiceCollection()
                .AddSingletonAs(c => new ServiceWrapped())
                    .As<IInterface1>()
                    .As<IInterface2>()
                    .AsSelf()
                    .Done()
                .BuildServiceProvider();

        AssetService(services, true);
    }

    [Fact]
    public void Should_resolve_scoped_with_as_registration()
    {
        var services =
            new ServiceCollection()
                .AddScopedAs<ServiceWrapped>()
                    .As<IInterface1>()
                    .As<IInterface2>()
                    .AsSelf()
                    .Done()
                .BuildServiceProvider();

        AssetService(services, true);
    }

    [Fact]
    public void Should_resolve_scoped_with_as_registration_using_factory_registration()
    {
        var services =
            new ServiceCollection()
                .AddSingletonAs(c => new ServiceWrapped())
                    .As<IInterface1>()
                    .As<IInterface2>()
                    .AsSelf()
                    .Done()
                .BuildServiceProvider();

        AssetService(services, true);
    }

    [Fact]
    public void Should_resolve_transient_with_as_registration()
    {
        var services =
            new ServiceCollection()
                .AddTransientAs<ServiceWrapped>()
                    .As<IInterface1>()
                    .As<IInterface2>()
                    .AsSelf()
                    .Done()
                .BuildServiceProvider();

        AssetService(services, false);
    }

    [Fact]
    public void Should_resolve_transient_with_as_registration_using_factory_registration()
    {
        var services =
            new ServiceCollection()
                .AddSingletonAs(c => new ServiceWrapped())
                    .As<IInterface1>()
                    .As<IInterface2>()
                    .AsSelf()
                    .Done()
                .BuildServiceProvider();

        AssetService(services, false);
    }

    private static void AssetService(ServiceProvider services, bool isSame)
    {
        var resolved1 = services.GetRequiredService<ServiceWrapped>();
        var resolved2 = services.GetRequiredService<IInterface1>();
        var resolved3 = services.GetRequiredService<IInterface2>();

        Assert.IsType<ServiceWrapped>(resolved1);
        Assert.IsType<ServiceWrapped>(resolved2);
        Assert.IsType<ServiceWrapped>(resolved3);

        if (isSame)
        {
            Assert.Same(resolved1, resolved2);
            Assert.Same(resolved1, resolved3);
        }
    }
}
