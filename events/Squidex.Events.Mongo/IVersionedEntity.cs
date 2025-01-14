﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Events.Mongo;

public interface IVersionedEntity<T>
{
    T DocumentId { get; }

    long Version { get; }
}
