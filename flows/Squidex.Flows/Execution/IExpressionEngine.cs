// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Execution;

public interface IExpressionEngine
{
    bool Evaluate<T>(string? expression, T value);

    ValueTask<string?> RenderAsync<T>(string? expression, T value, ExpressionFallback fallback);

    string Serialize<T>(T value);
}
