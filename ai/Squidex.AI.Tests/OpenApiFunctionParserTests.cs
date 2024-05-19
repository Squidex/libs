// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FluentAssertions;
using FluentAssertions.Equivalency;
using OpenAI.ObjectModels.RequestModels;
using Squidex.AI.Implementation.OpenAI;
using Xunit;

namespace Squidex.AI;

public class OpenApiFunctionParserTests
{
    [Fact]
    public void Should_throw_if_required_value_is_null()
    {
        var arg = new ToolStringArgumentSpec("My String")
        {
            IsRequired = true
        };

        var spec = BuildSpec("string", arg);

        var call = new FunctionCall
        {
            Arguments = "{ \"string\": null }"
        };

        Assert.Throws<ChatException>(() => call.ParseArguments(spec));
    }

    [Fact]
    public void Should_throw_if_required_value_is_not_found()
    {
        var arg = new ToolStringArgumentSpec("My String")
        {
            IsRequired = true
        };

        var spec = BuildSpec("string", arg);

        var call = new FunctionCall
        {
            Arguments = "{ \"string\": null }"
        };

        Assert.Throws<ChatException>(() => call.ParseArguments(spec));
    }

    [Fact]
    public void Should_throw_if_type_does_not_match()
    {
        var arg = new ToolStringArgumentSpec("My String")
        {
            IsRequired = true
        };

        var spec = BuildSpec("string", arg);

        var call = new FunctionCall
        {
            Arguments = "{ \"string\": 42 }"
        };

        Assert.Throws<ChatException>(() => call.ParseArguments(spec));
    }

    [Fact]
    public void Should_parse_null()
    {
        var arg = new ToolStringArgumentSpec("My String");

        var spec = BuildSpec("string", arg);

        var call = new FunctionCall
        {
            Arguments = "{ \"string\": null }"
        };

        var parsed = call.ParseArguments(spec);

        parsed.Should().BeEquivalentTo(new Dictionary<string, ToolValue>
        {
            ["string"] = new ToolNullValue()
        }, IgnoreAsMethods);
    }

    [Fact]
    public void Should_parse_string()
    {
        var arg = new ToolStringArgumentSpec("My String");

        var spec = BuildSpec("string", arg);

        var call = new FunctionCall
        {
            Arguments = "{ \"string\": \"Hello\" }"
        };

        var parsed = call.ParseArguments(spec);

        parsed.Should().BeEquivalentTo(new Dictionary<string, ToolValue>
        {
            ["string"] = new ToolStringValue("Hello")
        }, IgnoreAsMethods);
    }

    [Fact]
    public void Should_parse_true()
    {
        var arg = new ToolBooleanArgumentSpec("My Boolean");

        var spec = BuildSpec("boolean", arg);

        var call = new FunctionCall
        {
            Arguments = "{ \"boolean\": true }"
        };

        var parsed = call.ParseArguments(spec);

        parsed.Should().BeEquivalentTo(new Dictionary<string, ToolValue>
        {
            ["boolean"] = new ToolBooleanValue(true)
        }, IgnoreAsMethods);
    }

    [Fact]
    public void Should_parse_false()
    {
        var arg = new ToolBooleanArgumentSpec("My Boolean");

        var spec = BuildSpec("boolean", arg);

        var call = new FunctionCall
        {
            Arguments = "{ \"boolean\": false }"
        };

        var parsed = call.ParseArguments(spec);

        parsed.Should().BeEquivalentTo(new Dictionary<string, ToolValue>
        {
            ["boolean"] = new ToolBooleanValue(false)
        }, IgnoreAsMethods);
    }

    [Fact]
    public void Should_parse_number()
    {
        var arg = new ToolNumberArgumentSpec("My Number");

        var spec = BuildSpec("number", arg);

        var call = new FunctionCall
        {
            Arguments = "{ \"number\": 42 }"
        };

        var parsed = call.ParseArguments(spec);

        parsed.Should().BeEquivalentTo(new Dictionary<string, ToolValue>
        {
            ["number"] = new ToolNumberValue(42)
        }, IgnoreAsMethods);
    }

    [Fact]
    public void Should_parse_enum()
    {
        var arg = new ToolEnumArgumentSpec("My Enum")
        {
            Values = ["A", "B"]
        };

        var spec = BuildSpec("enum", arg);

        var call = new FunctionCall
        {
            Arguments = "{ \"enum\": \"A\" }"
        };

        var parsed = call.ParseArguments(spec);

        parsed.Should().BeEquivalentTo(new Dictionary<string, ToolValue>
        {
            ["enum"] = new ToolStringValue("A")
        }, IgnoreAsMethods);
    }

    [Fact]
    public void Should_throw_if_enum_has_invalid_value()
    {
        var arg = new ToolEnumArgumentSpec("My Enum")
        {
            Values = ["A", "B"]
        };

        var spec = BuildSpec("enum", arg);

        var call = new FunctionCall
        {
            Arguments = "{ \"enum\": \"C\" }"
        };

        Assert.Throws<ChatException>(() => call.ParseArguments(spec));
    }

    private static ToolSpec BuildSpec(string name, ToolArgumentSpec spec)
    {
        return new ToolSpec("function", "function", "My Function")
        {
            Arguments =
            {
                [name] = spec
            }
        };
    }

    private static EquivalencyAssertionOptions<Dictionary<string, ToolValue>> IgnoreAsMethods(EquivalencyAssertionOptions<Dictionary<string, ToolValue>> options)
    {
        return options.Excluding(x => x.Name.StartsWith("As", StringComparison.Ordinal));
    }
}
