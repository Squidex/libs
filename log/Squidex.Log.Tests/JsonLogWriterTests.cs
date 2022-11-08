// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Log;

public class JsonLogWriterTests
{
    private readonly IRootWriter sut = JsonLogWriterFactory.Default().Create();

    public JsonLogWriterTests()
    {
        sut.Start();
    }

    [Fact]
    public void Should_write_boolean_property()
    {
        sut.WriteProperty("property", true);

        Assert.Equal(@"{""property"":true}", sut.End());
    }

    [Fact]
    public void Should_write_long_property()
    {
        sut.WriteProperty("property", 120);

        Assert.Equal(@"{""property"":120}", sut.End());
    }

    [Fact]
    public void Should_write_double_property()
    {
        sut.WriteProperty("property", 1.5);

        Assert.Equal(@"{""property"":1.5}", sut.End());
    }

    [Fact]
    public void Should_write_string_property()
    {
        sut.WriteProperty("property", "my-string");

        Assert.Equal(@"{""property"":""my-string""}", sut.End());
    }

    [Fact]
    public void Should_write_duration_property()
    {
        sut.WriteProperty("property", new TimeSpan(2, 16, 30, 20, 100));

        Assert.Equal(@"{""property"":""2.16:30:20.1000000""}", sut.End());
    }

    [Fact]
    public void Should_write_datetime_property()
    {
        var value = new DateTime(2012, 11, 10, 9, 8, 45, DateTimeKind.Utc);

        sut.WriteProperty("property", value);

        Assert.Equal(@"{""property"":""2012-11-10T09:08:45Z""}", sut.End());
    }

    [Fact]
    public void Should_write_datetime_offset_property()
    {
        var value = new DateTimeOffset(2012, 11, 10, 11, 8, 45, TimeSpan.FromHours(2));

        sut.WriteProperty("property", value);

        Assert.Equal(@"{""property"":""2012-11-10T09:08:45Z""}", sut.End());
    }

    [Fact]
    public void Should_write_boolean_value()
    {
        sut.WriteArray("property", a => a.WriteValue(true));

        Assert.Equal(@"{""property"":[true]}", sut.End());
    }

    [Fact]
    public void Should_write_long_value()
    {
        sut.WriteArray("property", a => a.WriteValue(120));

        Assert.Equal(@"{""property"":[120]}", sut.End());
    }

    [Fact]
    public void Should_write_double_value()
    {
        sut.WriteArray("property", a => a.WriteValue(1.5));

        Assert.Equal(@"{""property"":[1.5]}", sut.End());
    }

    [Fact]
    public void Should_write_string_value()
    {
        sut.WriteArray("property", a => a.WriteValue("my-string"));

        Assert.Equal(@"{""property"":[""my-string""]}", sut.End());
    }

    [Fact]
    public void Should_write_duration_value()
    {
        sut.WriteArray("property", a => a.WriteValue(new TimeSpan(2, 16, 30, 20, 100)));

        Assert.Equal(@"{""property"":[""2.16:30:20.1000000""]}", sut.End());
    }

    [Fact]
    public void Should_write_object_in_array()
    {
        sut.WriteArray("property1", a => a.WriteObject(b => b.WriteProperty("property2", 120)));

        Assert.Equal(@"{""property1"":[{""property2"":120}]}", sut.End());
    }

    [Fact]
    public void Should_write_object_in_array_with_context()
    {
        sut.WriteArray("property1", a => a.WriteObject(13, (ctx, b) => b.WriteProperty("property2", 13)));

        Assert.Equal(@"{""property1"":[{""property2"":13}]}", sut.End());
    }

    [Fact]
    public void Should_write_array_value_with_context()
    {
        sut.WriteArray("property", 13, (ctx, a) => a.WriteValue(ctx));

        Assert.Equal(@"{""property"":[13]}", sut.End());
    }

    [Fact]
    public void Should_write_datetime_value()
    {
        var value = new DateTime(2012, 11, 10, 9, 8, 45, DateTimeKind.Utc);

        sut.WriteArray("property", a => a.WriteValue(value));

        Assert.Equal(@"{""property"":[""2012-11-10T09:08:45Z""]}", sut.End());
    }

    [Fact]
    public void Should_write_datetime_offset_value()
    {
        var value = new DateTimeOffset(2012, 11, 10, 11, 8, 45, TimeSpan.FromHours(2));

        sut.WriteArray("property", a => a.WriteValue(value));

        Assert.Equal(@"{""property"":[""2012-11-10T09:08:45Z""]}", sut.End());
    }

    [Fact]
    public void Should_write_nested_object()
    {
        sut.WriteObject("property", o => o.WriteProperty("nested", "my-string"));

        Assert.Equal(@"{""property"":{""nested"":""my-string""}}", sut.End());
    }

    [Fact]
    public void Should_write_nested_object_with_context()
    {
        sut.WriteObject("property", 13, (ctx, o) => o.WriteProperty("nested", ctx));

        Assert.Equal(@"{""property"":{""nested"":13}}", sut.End());
    }

    [Fact]
    public void Should_write_pretty_json()
    {
        var prettySut = new JsonLogWriterFactory(true).Create();

        prettySut.Start();
        prettySut.WriteProperty("property", 1.5);

        Assert.Equal(@"{NL  ""property"": 1.5NL}".Replace("NL", Environment.NewLine, StringComparison.Ordinal), prettySut.End());
    }

    [Fact]
    public void Should_write_extra_line_after_object()
    {
        var prettySut = new JsonLogWriterFactory(false, true).Create();

        prettySut.Start();
        prettySut.WriteProperty("property", 1.5);

        Assert.Equal(@"{""property"":1.5}NL".Replace("NL", Environment.NewLine, StringComparison.Ordinal), prettySut.End());
    }
}
