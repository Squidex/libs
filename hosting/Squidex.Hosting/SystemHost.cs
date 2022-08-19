// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Log;

namespace Squidex.Hosting
{
    public abstract class SystemHost<T> where T : ISystem
    {
        protected IReadOnlyList<(T System, string Name)> Systems { get; }

        protected ISemanticLog Log { get; }

        protected SystemHost(ISemanticLog log, IEnumerable<T> systems)
        {
            Log = log;

            Systems =
                systems.Distinct()
                    .Select(x => (System: x, Name: GetName(x)))
                    .OrderBy(x => x.System.Order)
                    .ThenBy(x => x.Name)
                    .ToList();
        }

        private static string GetName(T system)
        {
            if (!string.IsNullOrWhiteSpace(system.Name))
            {
                return system.Name;
            }

            return system.GetType().Name;
        }
    }
}
