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

public sealed class TestMessage(Guid testId, int value) : BaseMessage(testId)
{
    public int Value { get; } = value;
}

public abstract class BaseMessage(Guid testId)
{
    public Guid TestId { get; } = testId;
}
