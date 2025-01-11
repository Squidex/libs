// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.EntityFramework;

public class EFMessagingDataEntity
{
    public string Group { get; set; }

    public string Key { get; set; }

    public string ValueType { get; set; }

    public string? ValueFormat { get; set; }

    public byte[] ValueData { get; set; }

    public DateTime Expiration { get; set; }
}
