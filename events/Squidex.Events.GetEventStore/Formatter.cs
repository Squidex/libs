// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using System.Text;
using EventStore.Client;
using EventStoreData = EventStore.Client.EventData;

namespace Squidex.Events.GetEventStore;

public static class Formatter
{
    private static readonly HashSet<string> PrivateHeaders = ["$v", "$p", "$c", "$causedBy"];

    public static StoredEvent Read(ResolvedEvent resolvedEvent, string? prefix)
    {
        var eventSource = resolvedEvent.Event;
        var eventPayload = Encoding.UTF8.GetString(eventSource.Data.Span);
        var eventHeaders = GetHeaders(eventSource);
        var eventData = new EventData(eventSource.EventType, eventHeaders, eventPayload);

        var streamName = GetStreamName(prefix, eventSource);

        return new StoredEvent(
            streamName,
            resolvedEvent.OriginalEventNumber.ToInt64().ToString(CultureInfo.InvariantCulture),
            eventSource.EventNumber.ToInt64(),
            eventData);
    }

    private static string GetStreamName(string? prefix, EventRecord @event)
    {
        var streamName = @event.EventStreamId;

        if (prefix != null && streamName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            streamName = streamName[(prefix.Length + 1)..];
        }

        return streamName;
    }

    private static EnvelopeHeaders GetHeaders(EventRecord @event)
    {
        var headers = EnvelopeHeaders.DeserializeFromJson(@event.Metadata.Span);

        foreach (var key in headers.Keys.ToList())
        {
            if (PrivateHeaders.Contains(key))
            {
                headers.Remove(key);
            }
        }

        return headers;
    }

    public static EventStoreData Write(EventData eventData)
    {
        var payload = Encoding.UTF8.GetBytes(eventData.Payload);

        var headersJson = eventData.Headers.SerializeToJsonBytes();
        var headersBytes = headersJson;

        return new EventStoreData(Uuid.FromGuid(Guid.NewGuid()), eventData.Type, payload, headersBytes);
    }
}
