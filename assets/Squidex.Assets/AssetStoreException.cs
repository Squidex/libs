// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

[Serializable]
public class AssetStoreException : Exception
{
    public AssetStoreException()
    {
    }

    public AssetStoreException(string message)
        : base(message)
    {
    }

    public AssetStoreException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
