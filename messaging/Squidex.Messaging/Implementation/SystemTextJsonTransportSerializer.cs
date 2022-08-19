// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;

namespace Squidex.Messaging.Implementation
{
    public sealed class SystemTextJsonTransportSerializer : ITransportSerializer
    {
        private readonly JsonSerializerOptions? options;

        public SystemTextJsonTransportSerializer(JsonSerializerOptions? options)
        {
            this.options = options;
        }

        public SystemTextJsonTransportSerializer(Action<JsonSerializerOptions> configure)
        {
            options = new JsonSerializerOptions();

            configure?.Invoke(options);
        }

        public object? Deserialize(byte[] data, Type type)
        {
            using var streamBuffer = new MemoryStream(data);

            return JsonSerializer.Deserialize(streamBuffer, type, options);
        }

        public byte[] Serialize(object? value)
        {
            using var streamBuffer = new MemoryStream();

            JsonSerializer.Serialize(streamBuffer, value, options);

            return streamBuffer.ToArray();
        }
    }
}
