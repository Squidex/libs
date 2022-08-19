// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Squidex.Assets;

#pragma warning disable MA0048 // File name must match type name

namespace Benchmarks
{
    [SimpleJob]
    [MemoryDiagnoser]
    public class Resizing
    {
        private IAssetThumbnailGenerator generator;
        private Stream source;
        private MemoryStream destination;

        [Params(
            typeof(ImageSharpThumbnailGenerator))]
        public Type Implementation { get; set; }

        [Params(
            "file_example_JPG_1MB.jpg",
            "file_example_PNG_1MB.png")]
        public string File { get; set; }

        [GlobalSetup]
        public void Prepare()
        {
            generator = (IAssetThumbnailGenerator)Activator.CreateInstance(Implementation)!;

            source = new FileStream(File, FileMode.Open);
        }

        [IterationSetup]
        public void Iteration()
        {
            source.Position = 0;

            destination = new MemoryStream();
        }

        [Benchmark]
        public async Task Resize()
        {
            await generator.CreateThumbnailAsync(source, "image/png", destination, new ResizeOptions
            {
                TargetHeight = 100,
                TargetWidth = 100
            });
        }

        [Benchmark]
        public async Task GetInfo()
        {
            await generator.GetImageInfoAsync(source, "image/png");
        }
    }

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Contains("--resize"))
            {
                var generator = new ImageSharpThumbnailGenerator();

                await using (var source = new FileStream("file_example_PNG_1MB.png", FileMode.Open))
                {
                    await using (var destination = new FileStream("resized.png", FileMode.Create))
                    {
                        await generator.CreateThumbnailAsync(source, "image/png", destination, new ResizeOptions
                        {
                            TargetHeight = 100,
                            TargetWidth = 100
                        });
                    }
                }
            }
            else
            {
                BenchmarkRunner.Run(typeof(Program).Assembly);
            }
        }
    }
}
