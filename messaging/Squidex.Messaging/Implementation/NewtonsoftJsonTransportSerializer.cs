// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Newtonsoft.Json;

namespace Squidex.Messaging.Implementation
{
    public sealed class NewtonsoftJsonTransportSerializer : ITransportSerializer
    {
        private readonly JsonSerializer serializer;

        public NewtonsoftJsonTransportSerializer()
        {
            serializer = JsonSerializer.CreateDefault();
        }

        public NewtonsoftJsonTransportSerializer(JsonSerializerSettings settings)
        {
            serializer = JsonSerializer.CreateDefault(settings);
        }

        public object? Deserialize(byte[] data, Type type)
        {
            using var streamBuffer = new MemoryStream(data);
            using var streamReader = new StreamReader(streamBuffer, Encoding.UTF8);

            using var jsonReader = new JsonTextReader(streamReader);

            return serializer.Deserialize(jsonReader, type);
        }

        public byte[] Serialize(object? value)
        {
            using var streamBuffer = new MemoryStream();

            using (var streamWriter = new StreamWriter(streamBuffer, Encoding.UTF8, leaveOpen: true))
            {
                using var jsonWriter = new JsonTextWriter(streamWriter);

                serializer.Serialize(jsonWriter, value);

                streamBuffer.Position = 0;
            }

            return streamBuffer.ToArray();
        }
    }
}
