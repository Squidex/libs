// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Web;
using Squidex.Log;

namespace Squidex.Hosting
{
    public sealed class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingletonAs(_ => JsonLogWriterFactory.Readable())
                .As<IRootWriterFactory>();

            services.AddDefaultForwardRules();
            services.AddDefaultWebServices(configuration);

            services.AddSingletonAs<MyService1>();
            services.AddSingletonAs<MyService2>();

            services.AddInitializer();
            services.AddBackgroundProcesses();

            services.AddInitializer("Test 1", s =>
            {
                s.GetRequiredService<ISemanticLog>().LogInformation(w => w.WriteProperty("Initializer", 1));
            });

            services.AddInitializer<ISemanticLog>("Test 2", log =>
            {
                log.LogInformation(w => w.WriteProperty("Initializer", 2));
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDefaultPathBase();
            app.UseDefaultForwardRules();

            app.UseMiddleware<EmbedMiddleware>();

            app.UseWhen(c => c.IsIndex(), builder =>
            {
                app.UseHtmlTransform(new HtmlTransformOptions
                {
                    Transform = (html, context) =>
                    {
                        html = html.Replace("<body>", "<body><script>console.log('test')</script>", StringComparison.OrdinalIgnoreCase);

                        return new ValueTask<string>(html);
                    }
                });
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/hello", async context =>
                {
                    await context.Response.WriteAsync("Hello, World!");
                });
            });

            app.UseStaticFiles();
        }
    }
}
