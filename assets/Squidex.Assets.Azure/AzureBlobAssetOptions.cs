// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public sealed class AzureBlobAssetOptions
{
    public string ConnectionString { get; set; }

    public string ContainerName { get; set; }
}
