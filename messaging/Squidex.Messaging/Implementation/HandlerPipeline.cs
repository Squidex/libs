// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Reflection;

namespace Squidex.Messaging.Implementation
{
    internal sealed class HandlerPipeline
    {
        private readonly Dictionary<Type, List<Func<object, CancellationToken, Task>>> handlersByType = new Dictionary<Type, List<Func<object, CancellationToken, Task>>>();

        public bool HasHandlers => handlersByType.Count > 0;

        public HandlerPipeline(IEnumerable<IMessageHandler> handlers)
        {
            foreach (var handler in handlers)
            {
                var type = handler.GetType();

                var handlerMethods =
                    type.GetMethods()
                        .Where(x =>
                            x.Name == "HandleAsync" &&
                            x.ReturnType == typeof(Task) &&
                            x.GetParameters().Length == 2 &&
                            x.GetParameters()[1].ParameterType == typeof(CancellationToken));

                foreach (var method in handlerMethods)
                {
                    var messageType = method.GetParameters()[0].ParameterType;
                    var messageBuilder = GetType().GetMethod(nameof(BuildCaller), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(messageType);

                    var builtDelegate = messageBuilder.Invoke(null, new object[] { handler });

                    if (builtDelegate is Func<object, CancellationToken, Task> typed)
                    {
                        if (!handlersByType.TryGetValue(messageType, out var list))
                        {
                            list = new List<Func<object, CancellationToken, Task>>();

                            handlersByType.Add(messageType, list);
                        }

                        list.Add(typed);
                    }
                }
            }
        }

        public IEnumerable<Func<object, CancellationToken, Task>> GetHandlers(Type type)
        {
            if (handlersByType.TryGetValue(type, out var handlers))
            {
                return handlers;
            }

            return Enumerable.Empty<Func<object, CancellationToken, Task>>();
        }

        private static Func<object, CancellationToken, Task>? BuildCaller<T>(IMessageHandler handler)
        {
            if (handler is not IMessageHandler<T> typed)
            {
                return null;
            }

            return (message, ct) =>
            {
                if (message is T typedMessage)
                {
                    return typed.HandleAsync(typedMessage, ct);
                }

                return Task.CompletedTask;
            };
        }
    }
}
