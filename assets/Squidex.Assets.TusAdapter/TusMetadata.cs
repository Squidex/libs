// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets.TusAdapter;

public sealed class TusMetadata
{
    public DateTimeOffset? Expires { get; set; }

    public string Id { get; set; }

    public string UploadMetadata { get; set; }

    public long? UploadLength { get; set; }

    public long WrittenBytes { get; set; }

    public int WrittenParts { get; set; }

    public bool Created { get; set; }
}
