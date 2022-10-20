// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;

namespace Squidex.Messaging.Implementation
{
    public sealed class SystemTextJsonMessagingSerializer : MessagingSerializerBase
    {
        private readonly JsonSerializerOptions? options;

        protected override string Format => "text/json";

        public SystemTextJsonMessagingSerializer(JsonSerializerOptions? options = null)
        {
            this.options = options;
        }

        public SystemTextJsonMessagingSerializer(Action<JsonSerializerOptions> configure)
        {
            options = new JsonSerializerOptions();

            configure?.Invoke(options);
        }

        protected override object? DeserializeCore(byte[] data, Type type)
        {
            using var streamBuffer = new MemoryStream(data);

            return JsonSerializer.Deserialize(streamBuffer, type, options);
        }

        protected override byte[] SerializeCore(object message)
        {
            using var streamBuffer = new MemoryStream();

            JsonSerializer.Serialize(streamBuffer, message, options);

            return streamBuffer.ToArray();
        }
    }
}
