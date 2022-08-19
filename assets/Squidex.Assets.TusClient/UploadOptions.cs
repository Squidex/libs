// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public struct UploadOptions
    {
        public string? FileId { get; set; }

        public Dictionary<string, string>? Metadata { get; set; }

        public IProgressHandler? ProgressHandler { get; set; }
    }
}
