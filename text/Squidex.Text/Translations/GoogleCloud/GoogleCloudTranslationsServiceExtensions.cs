// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Text.Translations;
using Squidex.Text.Translations.GoogleCloud;

namespace Microsoft.Extensions.DependencyInjection;

public static class GoogleCloudTranslationsServiceExtensions
{
    public static IServiceCollection AddGoogleCloudTranslations(this IServiceCollection services, IConfiguration config, Action<GoogleCloudTranslationOptions>? configure = null,
        string configPath = "translations:googleCloud")
    {
        services.Configure(config, configPath, configure);

        services.AddTranslations();
        services.AddSingletonAs<GoogleCloudTranslationService>()
            .As<ITranslationService>().AsSelf();

        return services;
    }
}
