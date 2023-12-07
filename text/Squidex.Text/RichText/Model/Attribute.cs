// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Text.RichText.Model;

public record struct Attribute(AttributeKind Kind, object? Value)
{
    public double AsNumber
    {
        get
        {
            if (Value is not double result)
            {
                ThrowInvalidKind(AttributeKind.Number);
                return default!;
            }

            return result;
        }
    }

    public string AsString
    {
        get
        {
            if (Value is not string result)
            {
                ThrowInvalidKind(AttributeKind.String);
                return default!;
            }

            return result;
        }
    }

    private void ThrowInvalidKind(AttributeKind expected)
    {
        var message = $"Expected '{expected}', got '{Kind}'.";

        throw new InvalidOperationException(message);
    }
}
