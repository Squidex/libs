// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

namespace Squidex.Flows;

public class FlowOptionsTests
{
    [Fact]
    public void Should_not_return_error_for_default_options()
    {
        var sut = new FlowOptions();

        Assert.Empty(sut.Validate());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(65)]
    public void Should_return_error_if_NumTasks_is_invalid(int numTasks)
    {
        var sut = new FlowOptions { NumTasks = numTasks };

        var error = sut.Validate().First();

        Assert.Equal(new ConfigurationError("Value must be between 1 and 64.", "NumTasks"), error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(65)]
    public void Should_return_error_if_NumWorker_is_invalid(int numWorker)
    {
        var sut = new FlowOptions { NumWorker = numWorker };

        var error = sut.Validate().First();

        Assert.Equal(new ConfigurationError("Value must be between 1 and 64.", "NumWorker"), error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(10_001)]
    public void Should_return_error_if_NumPartitions_is_invalid(int numPartitions)
    {
        var sut = new FlowOptions { NumPartitions = numPartitions };

        var error = sut.Validate().First();

        Assert.Equal(new ConfigurationError("Value must be between 1 and 10_000.", "NumPartitions"), error);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(-1)]
    [InlineData(1)]
    public void Should_return_error_if_WorkerIndex_is_invalid(int value)
    {
        var sut = new FlowOptions { WorkerIndex = value };

        var error = sut.Validate().First();

        Assert.Equal(new ConfigurationError("Value must be between 0 and 0 (NumWorker).", "WorkerIndex"), error);
    }

    [Fact]
    public void Should_return_error_if_NumWorker_is_not_a_multiple_of_num_partitions()
    {
        var sut = new FlowOptions { NumPartitions = 10, NumWorker = 4 };

        var error = sut.Validate().First();

        Assert.Equal(new ConfigurationError("Value must be a multiple of the number of workers.", "NumPartitions"), error);
    }

    [Fact]
    public void Should_compute_the_partition_from_keys()
    {
        var sut = new FlowOptions();

        var partition1 = sut.GetPartition("Key1");
        var partition2 = sut.GetPartition("Key2");

        Assert.NotEqual(partition1, partition2);
        Assert.True(partition1 >= 0);
        Assert.True(partition2 >= 0);
    }

    [Fact]
    public void Should_get_partitions()
    {
        var sut = new FlowOptions { NumPartitions = 12, NumWorker = 4 };

        var errors = sut.Validate();
        Assert.Empty(errors);

        sut.WorkerIndex = 0;
        var partitions0 = sut.GetPartitions();
        Assert.Equal([0, 1, 2], partitions0);

        sut.WorkerIndex = 1;
        var partitions1 = sut.GetPartitions();
        Assert.Equal([3, 4, 5], partitions1);

        sut.WorkerIndex = 2;
        var partitions2 = sut.GetPartitions();
        Assert.Equal([6, 7, 8], partitions2);

        sut.WorkerIndex = 3;
        var partitions3 = sut.GetPartitions();
        Assert.Equal([9, 10, 11], partitions3);
    }
}
