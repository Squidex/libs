// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text;

public class CasingTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("m", "m")]
    [InlineData("m y", "m-y")]
    [InlineData("M Y", "m-y")]
    [InlineData("M_Y", "m-y")]
    [InlineData("M_Y ", "m-y")]
    public void Should_convert_to_kebap_case(string input, string output)
    {
        Assert.Equal(output, input.ToKebabCase());
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("m", "M")]
    [InlineData("m-y", "MY")]
    [InlineData("my", "My")]
    [InlineData("myProperty ", "MyProperty")]
    [InlineData("my property", "MyProperty")]
    [InlineData("my_property", "MyProperty")]
    [InlineData("my-property", "MyProperty")]
    public void Should_convert_to_pascal_case(string input, string output)
    {
        Assert.Equal(output, input.ToPascalCase());
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("M", "m")]
    [InlineData("My", "my")]
    [InlineData("M-y", "mY")]
    [InlineData("MyProperty ", "myProperty")]
    [InlineData("My property", "myProperty")]
    [InlineData("My_property", "myProperty")]
    [InlineData("My-property", "myProperty")]
    public void Should_convert_to_camel_case(string input, string output)
    {
        Assert.Equal(output, input.ToCamelCase());
    }
}
