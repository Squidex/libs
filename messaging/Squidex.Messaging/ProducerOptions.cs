// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging;

public class ProducerOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan Expires { get; set; } = TimeSpan.FromHours(1);
}
