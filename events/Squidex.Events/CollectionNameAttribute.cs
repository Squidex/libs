﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Events;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CollectionNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}