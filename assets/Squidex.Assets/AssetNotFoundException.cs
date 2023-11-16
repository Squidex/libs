// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.Internal;

namespace Squidex.Assets;

[Serializable]
public class AssetNotFoundException : Exception
{
    public AssetNotFoundException(string fileName, Exception? inner = null)
        : base(FormatMessage(fileName), inner)
    {
    }

    private static string FormatMessage(string fileName)
    {
        Guard.NotNullOrEmpty(fileName, nameof(fileName));

        return $"An asset with name '{fileName}' does not exist.";
    }
}
