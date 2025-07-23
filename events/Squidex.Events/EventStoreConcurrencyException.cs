// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Events;

public sealed class EventStoreConcurrencyException(string message, Exception? inner = null)
    : Exception(message, inner)
{
}
