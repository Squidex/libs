// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Internal.Execution;

public interface IFlowExpressionEngine
{
    bool Evaluate<T>(string? expression, T value);

    ValueTask<string?> RenderAsync<T>(string? expression, T value, ExpressionFallback fallback = default);

    string SerializeJson<T>(T value);

    T DeserializeJson<T>(string json);
}
