// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging;

public sealed class RoutingCollection : List<(Func<object, bool> Predicate, ChannelName Channel)>
{
    public RoutingCollection()
    {
    }

    public RoutingCollection(RoutingCollection source)
        : base(source)
    {
    }

    public void Add(Func<object, bool> predicate, string name, ChannelType type = ChannelType.Queue)
    {
        Add((predicate, new ChannelName(name, type)));
    }

    public void Add(Func<object, bool> predicate, ChannelName channel)
    {
        Add((predicate, channel));
    }

    public void AddFallback(string name, ChannelType type = ChannelType.Queue)
    {
        Add((x => true, new ChannelName(name, type)));
    }

    public void AddFallback(ChannelName channel)
    {
        Add((x => true, channel));
    }
}
