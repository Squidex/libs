// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

namespace Squidex.Flows;

public sealed class FlowOptions : IValidatableOptions
{
    public TimeSpan JobQueryInterval { get; set; } = TimeSpan.FromSeconds(10);

    public List<Type> Steps { get; set; } = [];

    public int NumTasks { get; set; } = 32;

    public int NumWorker { get; set; } = 1;

    public int NumPartitions { get; set; } = 120;

    public int WorkerIndex { get; set; }

    public void AddStepIfNotExist(Type stepType)
    {
        Steps.Remove(stepType);
        Steps.Add(stepType);
    }

    public int GetPartition(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return Math.Abs(key.GetHashCode(StringComparison.InvariantCulture) % NumPartitions);
    }

    public int[] GetPartitions()
    {
        var partitionsPerWorker = NumPartitions / NumWorker;

        return Enumerable.Range(partitionsPerWorker * WorkerIndex, partitionsPerWorker).ToArray();
    }

    public IEnumerable<ConfigurationError> Validate()
    {
        if (NumTasks <= 0 || NumTasks > 64)
        {
            yield return new ConfigurationError("Value must be between 1 and 64.", nameof(NumTasks));
        }

        if (NumWorker <= 0 || NumWorker > 64)
        {
            yield return new ConfigurationError("Value must be between 1 and 64.", nameof(NumWorker));
        }

        if (NumPartitions <= 0 || NumPartitions > 10_000)
        {
            yield return new ConfigurationError("Value must be between 1 and 10_000.", nameof(NumPartitions));
        }

        if (WorkerIndex < 0 || WorkerIndex >= NumWorker)
        {
            yield return new ConfigurationError($"Value must be between 0 and {NumWorker - 1} (NumWorker).", nameof(WorkerIndex));
        }

        if (NumPartitions % NumWorker != 0)
        {
            yield return new ConfigurationError("Value must be a multiple of the number of workers.", nameof(NumPartitions));
        }
    }
}
