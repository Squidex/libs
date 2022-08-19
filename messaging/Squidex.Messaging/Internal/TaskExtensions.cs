// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Internal
{
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
                task.ContinueWith(
                    IgnoreTaskContinuation,
                    default,
                    TaskContinuationOptions.OnlyOnFaulted |
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }
    }
}
