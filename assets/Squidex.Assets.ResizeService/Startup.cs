// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Http.Features;

namespace Squidex.Assets.ResizeService;

public sealed class Startup
{
    private readonly IConfiguration configuration;

    public Startup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var options = configuration.GetSection("images").Get<ImageResizeOptions>()!;

        services.AddHealthChecks();
        services.AddDefaultForwardRules();
        services.AddDefaultWebServices(configuration);
        services.AddSingleton<ImageResizer>();

        services.AddSingletonAs(c => new CompositeThumbnailGenerator(new IAssetThumbnailGenerator[]
        {
            new ImageSharpThumbnailGenerator(),
            new ImageMagickThumbnailGenerator()
        }, options.MaxTasks)).As<IAssetThumbnailGenerator>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDefaultPathBase();
        app.UseDefaultForwardRules();

        app.UseRouting();
        app.UseHealthChecks("/healthz");

        var resizer = app.ApplicationServices.GetRequiredService<ImageResizer>();

        app.Use((context, next) =>
        {
            var requestSizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();

            if (requestSizeFeature != null)
            {
                requestSizeFeature.MaxRequestBodySize = null;
            }

            return next();
        });

        app.UseEndpoints(endpoints =>
        {
            resizer.Map(endpoints);
        });
    }
}
