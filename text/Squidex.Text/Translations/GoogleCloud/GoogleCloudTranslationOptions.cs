// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.Translations.GoogleCloud
{
    public sealed class GoogleCloudTranslationOptions
    {
        public string ProjectId { get; set; }

        public Dictionary<string, string> Mapping { get; set; }
    }
}
