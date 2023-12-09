// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Model;

public interface IAttributed
{
    int GetIntAttr(string name, int defaultValue = 0);

    string GetStringAttr(string name, string defaultValue = "");
}
