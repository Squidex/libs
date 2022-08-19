// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging
{
    public interface ITransportSerializer
    {
        object? Deserialize(byte[] data, Type type);

        byte[] Serialize(object? value);
    }
}
