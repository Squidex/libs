// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting
{
    public abstract class SystemBase : ISystem
    {
        public string Name { get; }

        public int Order { get; }

        protected SystemBase(string name, int order)
        {
            Name = name;

            Order = order;
        }
    }
}
