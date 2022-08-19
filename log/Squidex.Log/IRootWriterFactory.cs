// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Log
{
    public interface IRootWriterFactory
    {
        IRootWriter Create();

        void Release(IRootWriter writer);
    }
}
