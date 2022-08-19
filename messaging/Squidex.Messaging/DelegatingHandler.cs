// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging.Internal;

namespace Squidex.Messaging
{
    public sealed class DelegatingHandler<T> : IMessageHandler<T>
    {
        private readonly Func<T, CancellationToken, Task> action;

        public DelegatingHandler(Func<T, Task> action)
        {
            Guard.NotNull(action, nameof(action));

            this.action = (m, _) => action(m);
        }

        public DelegatingHandler(Func<T, CancellationToken, Task> action)
        {
            Guard.NotNull(action, nameof(action));

            this.action = action;
        }

        public Task HandleAsync(T message,
            CancellationToken ct)
        {
            return action(message, ct);
        }
    }
}
