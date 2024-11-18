// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

[Serializable]
public class AssetAlreadyExistsException(string fileName, Exception? inner = null) : Exception(FormatMessage(fileName), inner)
{
    private static string FormatMessage(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return $"An asset with name '{fileName}' already exists.";
    }
}
