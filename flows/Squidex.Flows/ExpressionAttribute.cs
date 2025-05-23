// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ExpressionAttribute(ExpressionFallback fallback = default) : Attribute
{
    public ExpressionFallback Fallback => fallback;
}
