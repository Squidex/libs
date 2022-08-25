// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Messaging
{
    public record struct SerializedObject(byte[] Data, string TypeString, string Format);

    public interface ITransportSerializer
    {
        (object Message, Type Type) Deserialize(SerializedObject source);

        SerializedObject Serialize(object message);
    }
}
