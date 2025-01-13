// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace Squidex.Events.EntityFramework;

public static class DefaultPools
{
    public static readonly ObjectPool<StringBuilder> StringBuilder =
        new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy());
}
