// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Implementation
{
    public sealed class RandomInstanceNameProvider : IInstanceNameProvider
    {
        public string Name { get; } = Guid.NewGuid().ToString();
    }
}
