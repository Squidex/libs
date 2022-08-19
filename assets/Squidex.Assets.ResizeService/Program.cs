// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets.ResizeService
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, builder) =>
                {
                    builder.ClearProviders();

                    builder.ConfigureSemanticLog(context.Configuration);
                })
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.ConfigureKestrel((context, serverOptions) =>
                    {
                        serverOptions.AllowSynchronousIO = true;

                        if (context.HostingEnvironment.IsDevelopment() || context.Configuration.GetValue<bool>("devMode:enable"))
                        {
                            serverOptions.ListenAnyIP(5005);
                        }
                    });

                    builder.UseStartup<Startup>();
                });
    }
}
