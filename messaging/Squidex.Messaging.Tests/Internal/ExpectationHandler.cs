// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;

namespace Squidex.Messaging.Internal;

internal sealed class ExpectationHandler : IMessageHandler<TestMessage>
{
    private readonly int expectCount;
    private readonly Guid expectedId;
    private readonly TaskCompletionSource tcs = new TaskCompletionSource();
    private readonly ConcurrentBag<int> messagesReceives = [];
    private readonly CancellationTokenSource cts;

    public Task Completion => tcs.Task;

    public IEnumerable<int> MessagesReceives => messagesReceives.OrderBy(x => x);

    public ExpectationHandler(int expectCount, Guid expectedId)
    {
        this.expectCount = expectCount;
        this.expectedId = expectedId;

        cts = new CancellationTokenSource(30 * 1000);

        cts.Token.Register(() =>
        {
            _ = tcs.TrySetResult();
        });
    }

    public Task HandleAsync(TestMessage message,
        CancellationToken ct)
    {
        if (message.TestId == expectedId)
        {
            messagesReceives.Add(message.Value);
        }

        if (expectCount == messagesReceives.Count)
        {
            tcs.TrySetResult();
        }

        return Task.CompletedTask;
    }
}
