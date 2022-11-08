// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FakeItEasy;
using Squidex.Messaging.Implementation;
using Xunit;

namespace Squidex.Messaging;

public sealed class HandlerPipelineTests
{
    public class Concrete : Base1, IInterface1
    {
    }

    public class Base1 : Base2
    {
    }

    public class Base2 : IInterface2
    {
    }

    public interface IInterface1
    {
    }

    public interface IInterface2
    {
    }

    [Fact]
    public void Should_return_concrete_handler()
    {
        var handler = A.Fake<IMessageHandler<Concrete>>();

        IsValidHandler(handler);
    }

    [Fact]
    public void Should_return_base1_handler()
    {
        var handler = A.Fake<IMessageHandler<Base1>>();

        IsValidHandler(handler);
    }

    [Fact]
    public void Should_return_base2_handler()
    {
        var handler = A.Fake<IMessageHandler<Base2>>();

        IsValidHandler(handler);
    }

    [Fact]
    public void Should_return_interface1_handler()
    {
        var handler = A.Fake<IMessageHandler<IInterface1>>();

        IsValidHandler(handler);
    }

    [Fact]
    public void Should_return_interface2_handler()
    {
        var handler = A.Fake<IMessageHandler<IInterface2>>();

        IsValidHandler(handler);
    }

    [Fact]
    public void Should_return_object_handler()
    {
        var handler = A.Fake<IMessageHandler<object>>();

        IsValidHandler(handler);
    }

    [Fact]
    public void Should_return_all_handlers()
    {
        var handlers = new IMessageHandler[]
        {
             A.Fake<IMessageHandler<Concrete>>(),
             A.Fake<IMessageHandler<Base1>>(),
             A.Fake<IMessageHandler<Base2>>(),
             A.Fake<IMessageHandler<IInterface1>>(),
             A.Fake<IMessageHandler<IInterface2>>(),
             A.Fake<IMessageHandler<object>>()
        };

        var sut = new HandlerPipeline(handlers);

        Assert.Equal(6, sut.GetHandlers(typeof(Concrete)).Count());
    }

    private static void IsValidHandler(IMessageHandler handler)
    {
        var sut = new HandlerPipeline(new[] { handler });

        Assert.NotNull(sut.GetHandlers(typeof(Concrete)).Single());
    }
}
