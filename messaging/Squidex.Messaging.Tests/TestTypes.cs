// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging;

public static class TestTypes
{
    public sealed class Message : BaseMessage
    {
        public int Value { get; }

        public Message(Guid testId, int value)
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
}
