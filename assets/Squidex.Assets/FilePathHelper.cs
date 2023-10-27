// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public static class FilePathHelper
{
    public static string EnsureThatPathIsChildOf(string path, string folder)
    {
        if (path.Contains("../", StringComparison.Ordinal) || path.Contains("..\\", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Names cannot point to parent directories.");
        }

        if (string.IsNullOrWhiteSpace(folder))
        {
            folder = "./";
        }

        var absolutePath = Path.GetFullPath(path);
        var absoluteFolder = Path.GetFullPath(folder);

        if (!absolutePath.StartsWith(absoluteFolder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Names cannot point to parent directories.");
        }

        return path;
    }
}
