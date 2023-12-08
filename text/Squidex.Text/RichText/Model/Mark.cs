// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Model;

public class Mark : MarkBase
{
    public MarkType Type { get; set; }

    public Attributes? Attributes { get; set; }

    public override MarkType GetMarkType()
    {
        return Type;
    }

    public override int GetIntAttr(string name, int defaultValue = 0)
    {
        if (Attributes?.TryGetValue(name, out var attr) == true && attr is int value)
        {
            return value;
        }

        return defaultValue;
    }

    public override string GetStringAttr(string name, string defaultValue = "")
    {
        if (Attributes?.TryGetValue(name, out var attr) == true && attr is string value)
        {
            return value;
        }

        return defaultValue;
    }
}
