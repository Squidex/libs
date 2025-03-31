// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Log;

namespace Squidex.Hosting;

public abstract class SystemHost<T>(ISemanticLog log, IEnumerable<T> systems) where T : ISystem
{
    protected IReadOnlyList<(T System, string Name)> Systems { get; } =
            systems.Distinct()
                .Select(x => (System: x, Name: GetName(x)))
                .OrderBy(x => x.System.Order)
                .ThenBy(x => x.Name)
                .ToList();

    protected ISemanticLog Log { get; } = log;

    private static string GetName(T system)
    {
        if (!string.IsNullOrWhiteSpace(system.Name))
        {
            return system.Name;
        }

        return system.GetType().Name;
    }
}
