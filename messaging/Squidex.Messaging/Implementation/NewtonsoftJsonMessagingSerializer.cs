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
    public sealed class NewtonsoftJsonMessagingSerializer : MessagingSerializerBase
    {
        private readonly JsonSerializer serializer;

        protected override string Format => "text/json";

        public NewtonsoftJsonMessagingSerializer()
        {
            serializer = JsonSerializer.CreateDefault();
        }

        public NewtonsoftJsonMessagingSerializer(JsonSerializerSettings settings)
        {
            serializer = JsonSerializer.CreateDefault(settings);
        }

        protected override object? DeserializeCore(byte[] data, Type type)
        {
            using var streamBuffer = new MemoryStream(data);
            using var streamReader = new StreamReader(streamBuffer, Encoding.UTF8);

            using var jsonReader = new JsonTextReader(streamReader);

            return serializer.Deserialize(jsonReader, type);
        }

        protected override byte[] SerializeCore(object message)
        {
            using var streamBuffer = new MemoryStream();

            using (var streamWriter = new StreamWriter(streamBuffer, Encoding.UTF8, leaveOpen: true))
            {
                using var jsonWriter = new JsonTextWriter(streamWriter);

                serializer.Serialize(jsonWriter, message);

                streamBuffer.Position = 0;
            }

            return streamBuffer.ToArray();
        }
    }
}
