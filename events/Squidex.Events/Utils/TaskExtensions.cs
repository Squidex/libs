// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Events.Utils;

public static class TaskExtensions
{
    private static readonly Action<Task> IgnoreTaskContinuation = t => { var ignored = t.Exception; };

    public static void Forget(this Task task)
    {
        if (task.IsCompleted)
        {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var ignored = task.Exception;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
        }
        else
        {
#pragma warning disable MA0134 // Observe result of async calls
            task.ContinueWith(
                IgnoreTaskContinuation,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
#pragma warning restore MA0134 // Observe result of async calls
        }
    }
}
