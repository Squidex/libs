// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Events;

public interface IEventSubscription : IDisposable
{
    void WakeUp();

    ValueTask CompleteAsync();
}
