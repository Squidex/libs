// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Internal;

#pragma warning disable MA0048 // File name must match type name

public sealed class TestValue(string value)
{
    public string Value { get; } = value;
}

public sealed class TestMessage : BaseMessage
{
    public int Value { get; }

    public TestMessage(Guid testId, int value)
        : base(testId)
    {
        Value = value;
    }
}

public abstract class BaseMessage
{
    public Guid TestId { get; }

    protected BaseMessage(Guid testId)
    {
        TestId = testId;
    }
}
