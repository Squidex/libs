// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Text.Translations;
using Squidex.Text.Translations.DeepL;

namespace Microsoft.Extensions.DependencyInjection;

public static class DeepLTranslationsServiceExtensions
{
    public static IServiceCollection AddDeepLTranslations(this IServiceCollection services, IConfiguration config, Action<DeepLTranslationOptions>? configure = null,
        string configPath = "translations:deepl")
    {
        services.Configure(config, configPath, configure);

        services.AddHttpClient("DeepL");
        services.AddTranslations();
        services.AddSingletonAs<DeepLTranslationService>()
            .As<ITranslationService>().AsSelf();

        return services;
    }
}
