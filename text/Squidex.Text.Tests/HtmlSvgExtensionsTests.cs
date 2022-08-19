// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.Properties;
using Xunit;

namespace Squidex.Text
{
    public class HtmlSvgExtensionsTests
    {
        [Fact]
        public void Should_return_true_for_valid_svg()
        {
            var isValid = Resources.SvgValid.IsValidSvg();

            Assert.True(isValid);
        }

        [Fact]
        public void Should_return_empty_errors_for_valid_svg()
        {
            var errors = Resources.SvgValid.GetSvgErrors();

            Assert.Empty(errors);
        }

        [Fact]
        public void Should_return_false_for_invalid_svg()
        {
            var isValid = Resources.SvgInvalid.IsValidSvg();

            Assert.False(isValid);
        }

        [Fact]
        public void Should_return_nonempty_errors_for_invalid_svg()
        {
            var errors = Resources.SvgInvalid.GetSvgErrors();

            Assert.NotEmpty(errors);
        }
    }
}
