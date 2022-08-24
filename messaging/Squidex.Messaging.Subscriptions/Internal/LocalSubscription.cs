﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions.Internal
{
    internal sealed class LocalSubscription<TMessage, TSubscription> : IObservable<TMessage>, IUntypedLocalSubscription, IDisposable
        where TSubscription : ISubscription
    {
        private readonly SubscriptionService subscriptions;
        private readonly TSubscription subscription;
        private IObserver<TMessage>? currentObserver;

        public Guid Id { get; } = Guid.NewGuid();

        public LocalSubscription(SubscriptionService subscriptions, TSubscription subscription)
        {
            this.subscriptions = subscriptions;
            this.subscription = subscription;
        }

        private void SubscribeCore(IObserver<TMessage> observer)
        {
            if (currentObserver != null)
            {
                throw new InvalidOperationException("Can only have one observer.");
            }

            subscriptions.SubscribeCore(Id, this, subscription);

            currentObserver = observer;
        }

        void IDisposable.Dispose()
        {
            if (currentObserver == null)
            {
                return;
            }

            subscriptions.UnsubscribeCore(Id);

            currentObserver = null;
        }

        public IDisposable Subscribe(IObserver<TMessage> observer)
        {
            SubscribeCore(observer);

            return this;
        }

        public void OnError(Exception error)
        {
            if (error != null)
            {
                currentObserver?.OnError(error);
            }
        }

        public void OnNext(object? value)
        {
            if (value is TMessage typed)
            {
                currentObserver?.OnNext(typed);
            }
        }
    }
}
