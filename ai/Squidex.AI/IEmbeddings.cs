// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI;

public interface IEmbeddings
{
    Task<ReadOnlyMemory<double>> CalculateEmbeddingsAsync(string query,
        CancellationToken ct);
}
