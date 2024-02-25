// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.ChatBots;

[Serializable]
public class ChatException : Exception
{
    public ChatException()
    {
    }

    public ChatException(string message)
        : base(message)
    {
    }

    public ChatException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
