// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.Serialization;

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

    protected AssetStoreException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
