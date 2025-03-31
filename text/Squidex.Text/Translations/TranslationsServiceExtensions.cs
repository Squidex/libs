// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection.Extensions;
using Squidex.Text.Translations;

namespace Microsoft.Extensions.DependencyInjection;

public static class TranslationsServiceExtensions
{
    public static IServiceCollection AddTranslations(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.TryAddSingleton<ITranslator, Translator>();

        return services;
    }
}
