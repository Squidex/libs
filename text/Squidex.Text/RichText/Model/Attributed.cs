// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Model;

public abstract class Attributed
{
    public abstract int GetIntAttr(string name, int defaultValue = 0);

    public abstract string GetStringAttr(string name, string defaultValue = "");
}
