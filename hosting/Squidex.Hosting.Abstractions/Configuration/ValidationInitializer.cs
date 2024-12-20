﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting.Configuration;

public sealed class ValidationInitializer(IEnumerable<IErrorProvider> errorProviders) : IInitializable
{
    public int Order => int.MinValue;

    public Task InitializeAsync(
        CancellationToken ct)
    {
        var errors = errorProviders.SelectMany(x => x.GetErrors()).ToList();

        if (errors.Count > 0)
        {
            throw new ConfigurationException(errors);
        }

        return Task.CompletedTask;
    }
}
