// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Squidex.Messaging.Internal
{
    public static class Guard
    {
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GreaterThan<TValue>(TValue target, TValue lower, string parameterName) where TValue : IComparable
        {
            if (target.CompareTo(lower) <= 0)
            {
                throw new ArgumentException($"Value must be greater than {lower}", parameterName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GreaterEquals<TValue>(TValue target, TValue lower, string parameterName) where TValue : IComparable
        {
            if (target.CompareTo(lower) < 0)
            {
                throw new ArgumentException($"Value must be greater or equal to {lower}", parameterName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull(object? target, string parameterName)
        {
            if (target == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNullOrEmpty(string? target, string parameterName)
        {
            NotNull(target, parameterName);

            if (string.IsNullOrWhiteSpace(target))
            {
                throw new ArgumentException("String parameter cannot be null or empty and cannot contain only blanks.", parameterName);
            }
        }
    }
}
