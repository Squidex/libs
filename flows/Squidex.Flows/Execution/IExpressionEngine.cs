// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Execution;

public interface IExpressionEngine
{
    bool Evaluate<TContext>(string expression, TContext context);

    string Execute<TContext>(string expression, TContext context);
}
