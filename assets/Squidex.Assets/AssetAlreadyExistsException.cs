// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

[Serializable]
public class AssetAlreadyExistsException : Exception
{
    public AssetAlreadyExistsException(string fileName, Exception? inner = null)
        : base(FormatMessage(fileName), inner)
    {
    }

    private static string FormatMessage(string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        return $"An asset with name '{fileName}' already exists.";
    }
}
