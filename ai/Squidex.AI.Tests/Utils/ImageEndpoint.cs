// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.AI.Implementation.OpenAI;

namespace Squidex.AI.Utils;

public sealed class ImageEndpoint : IHttpImageEndpoint
{
    public string GetUrl(string relativePath)
    {
        return $"https://localhost:5001/{relativePath}";
    }
}
