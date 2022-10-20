// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging.Internal;

namespace Squidex.Messaging.Implementation
{
    public abstract class MessagingSerializerBase : IMessagingSerializer
    {
        protected abstract string Format { get; }

        public bool IgnoreVersionInTypeString { get; set; } = true;

        public (object Message, Type Type) Deserialize(SerializedObject source)
        {
            var type = Type.GetType(source.TypeString);

            if (type == null)
            {
                ThrowHelper.ArgumentException("Invalid type information.", nameof(source.TypeString));
                return default!;
            }

            var message = DeserializeCore(source.Data, type);

            if (message == null)
            {
                ThrowHelper.InvalidOperationException("Deserialization returns null value.");
                return default!;
            }

            return (message, type);
        }

        protected abstract object? DeserializeCore(byte[] data, Type type);

        public SerializedObject Serialize(object? message)
        {
            if (message == null)
            {
                ThrowHelper.ArgumentException("Cannot serialize null message.", nameof(message));
                return default;
            }

            var typeString =
                IgnoreVersionInTypeString ?
                    message.GetType().GetShortTypeName() :
                    message.GetType().AssemblyQualifiedName;

            if (string.IsNullOrWhiteSpace(typeString))
            {
                ThrowHelper.ArgumentException("Cannot calculate type name.", nameof(message));
                return default;
            }

            var data = SerializeCore(message);

            if (data == null)
            {
                ThrowHelper.InvalidOperationException("Serialization returns null buffer.");
                return default!;
            }

            return new SerializedObject(data, typeString, Format);
        }

        protected abstract byte[] SerializeCore(object message);
    }
}
