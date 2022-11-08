// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging.Implementation;
using Xunit;

namespace Squidex.Messaging;

public class SerializerTests
{
    private class TestObject
    {
        public string Value { get; set; }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Should_serialize_newtonsoft(bool ignoreVersion)
    {
        var serializer = new NewtonsoftJsonMessagingSerializer { IgnoreVersionInTypeString = ignoreVersion };

        SerializeAndDeserialize(serializer);
    }

    [Fact]
    public void Should_throw_exception_when_serializing_null_newtonsoft()
    {
        var serializer = new NewtonsoftJsonMessagingSerializer();

        ThrowExceptionWhenSerializingNull(serializer);
    }

    [Fact]
    public void Should_throw_exception_when_desserializing_invalid_type_newtonsoft()
    {
        var serializer = new NewtonsoftJsonMessagingSerializer();

        ThrowExceptionWhenDeserializingInvalidType(serializer);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Should_serialize_sts(bool ignoreVersion)
    {
        var serializer = new SystemTextJsonMessagingSerializer { IgnoreVersionInTypeString = ignoreVersion };

        SerializeAndDeserialize(serializer);
    }

    [Fact]
    public void Should_throw_exception_when_serializing_null_sts()
    {
        var serializer = new SystemTextJsonMessagingSerializer();

        ThrowExceptionWhenSerializingNull(serializer);
    }

    [Fact]
    public void Should_throw_exception_when_desserializing_invalid_type_sts()
    {
        var serializer = new SystemTextJsonMessagingSerializer();

        ThrowExceptionWhenDeserializingInvalidType(serializer);
    }

    private static void ThrowExceptionWhenSerializingNull(IMessagingSerializer serializer)
    {
        var input = (object?)null;

        Assert.Throws<ArgumentException>(() => serializer.Serialize(input!));
    }

    private static void ThrowExceptionWhenDeserializingInvalidType(IMessagingSerializer serializer)
    {
        var input = new SerializedObject(Array.Empty<byte>(), "Invalid", "format");

        Assert.Throws<ArgumentException>(() => serializer.Deserialize(input!));
    }

    private static void SerializeAndDeserialize(IMessagingSerializer serializer)
    {
        var source = new TestObject { Value = Guid.NewGuid().ToString() };

        var valueWritten = serializer.Serialize(source);
        var valueRead = serializer.Deserialize(valueWritten);

        Assert.Equal(source.GetType(), valueRead.Type);
        Assert.Equal(source.Value, ((TestObject)valueRead.Message).Value);
    }
}
