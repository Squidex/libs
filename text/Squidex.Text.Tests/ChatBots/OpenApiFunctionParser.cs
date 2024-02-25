// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FluentAssertions;
using OpenAI.ObjectModels.RequestModels;
using Squidex.Text.ChatBots.OpenAI;
using Xunit;

namespace Squidex.Text.ChatBots;

public class OpenApiFunctionParser
{
    [Fact]
    public void Should_throw_if_required_value_is_null()
    {
        var spec = new ToolStringArgumentSpec("string", "My String")
        {
            IsRequired = true
        };

        var call = new FunctionCall
        {
            Arguments = "{ \"string\": null }"
        };

        Assert.Throws<ChatException>(() => call.ParseArguments(BuildSpec(spec)));
    }

    [Fact]
    public void Should_throw_if_required_value_is_not_found()
    {
        var spec = new ToolStringArgumentSpec("string", "My String")
        {
            IsRequired = true
        };

        var call = new FunctionCall
        {
            Arguments = "{ \"string\": null }"
        };

        Assert.Throws<ChatException>(() => call.ParseArguments(BuildSpec(spec)));
    }

    [Fact]
    public void Should_throw_if_type_does_not_match()
    {
        var spec = new ToolStringArgumentSpec("string", "My String")
        {
            IsRequired = true
        };

        var call = new FunctionCall
        {
            Arguments = "{ \"string\": 42 }"
        };

        Assert.Throws<ChatException>(() => call.ParseArguments(BuildSpec(spec)));
    }

    [Fact]
    public void Should_parse_null()
    {
        var spec = new ToolStringArgumentSpec("string", "My String");

        var call = new FunctionCall
        {
            Arguments = "{ \"string\": null }"
        };

        var parsed = call.ParseArguments(BuildSpec(spec));

        parsed.Should().BeEquivalentTo(new Dictionary<string, ToolValue>
        {
            ["string"] = new ToolNullValue()
        });
    }

    [Fact]
    public void Should_parse_string()
    {
        var spec = new ToolStringArgumentSpec("string", "My String");

        var call = new FunctionCall
        {
            Arguments = "{ \"string\": \"Hello\" }"
        };

        var parsed = call.ParseArguments(BuildSpec(spec));

        parsed.Should().BeEquivalentTo(new Dictionary<string, ToolValue>
        {
            ["string"] = new ToolStringValue("Hello")
        });
    }

    [Fact]
    public void Should_parse_true()
    {
        var spec = new ToolBooleanArgumentSpec("boolean", "My Boolean");

        var call = new FunctionCall
        {
            Arguments = "{ \"boolean\": true }"
        };

        var parsed = call.ParseArguments(BuildSpec(spec));

        parsed.Should().BeEquivalentTo(new Dictionary<string, ToolValue>
        {
            ["boolean"] = new ToolBooleanValue(true)
        });
    }

    [Fact]
    public void Should_parse_false()
    {
        var spec = new ToolBooleanArgumentSpec("boolean", "My Boolean");

        var call = new FunctionCall
        {
            Arguments = "{ \"boolean\": false }"
        };

        var parsed = call.ParseArguments(BuildSpec(spec));

        parsed.Should().BeEquivalentTo(new Dictionary<string, ToolValue>
        {
            ["boolean"] = new ToolBooleanValue(false)
        });
    }

    [Fact]
    public void Should_parse_number()
    {
        var spec = new ToolNumberArgumentSpec("number", "My Number");

        var call = new FunctionCall
        {
            Arguments = "{ \"number\": 42 }"
        };

        var parsed = call.ParseArguments(BuildSpec(spec));

        parsed.Should().BeEquivalentTo(new Dictionary<string, ToolValue>
        {
            ["number"] = new ToolNumberValue(42)
        });
    }

    [Fact]
    public void Should_parse_enum()
    {
        var spec = new ToolEnumArgumentSpec("enum", "My Enum")
        {
            Values = ["A", "B"]
        };

        var call = new FunctionCall
        {
            Arguments = "{ \"enum\": \"A\" }"
        };

        var parsed = call.ParseArguments(BuildSpec(spec));

        parsed.Should().BeEquivalentTo(new Dictionary<string, ToolValue>
        {
            ["enum"] = new ToolStringValue("A")
        });
    }

    [Fact]
    public void Should_throw_if_enum_has_invalid_value()
    {
        var spec = new ToolEnumArgumentSpec("enum", "My Enum")
        {
            Values = ["A", "B"]
        };

        var call = new FunctionCall
        {
            Arguments = "{ \"enum\": \"C\" }"
        };

        Assert.Throws<ChatException>(() => call.ParseArguments(BuildSpec(spec)));
    }

    private static ToolSpec BuildSpec(ToolArgumentSpec spec)
    {
        return new ToolSpec("function", "My Function")
        {
            Arguments = [spec]
        };
    }
}
