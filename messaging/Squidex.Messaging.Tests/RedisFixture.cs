// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using StackExchange.Redis;

namespace Squidex.Messaging;

public class RedisFixture
{
    public ConnectionMultiplexer Connection { get; }

    public RedisFixture()
    {
        Connection = ConnectionMultiplexer.Connect("localhost");
    }
}
