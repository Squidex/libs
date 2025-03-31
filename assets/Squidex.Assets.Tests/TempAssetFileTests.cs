// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Newtonsoft.Json;
using Xunit;

namespace Squidex.Assets;

public class TempAssetFileTests
{
    [Fact]
    public async Task Should_construct()
    {
        await using (var result = new TempAssetFile("fileName", "file/type"))
        {
            Assert.Equal("fileName", result.FileName);
            Assert.Equal("file/type", result.MimeType);
            Assert.Equal(0, result.FileSize);
        }
    }

    [Fact]
    public async Task Should_construct_from_other_file()
    {
        var source = new DelegateAssetFile("fileName", "file/type", 1024, () => new MemoryStream());

        await using (var result = TempAssetFile.Create(source))
        {
            Assert.Equal("fileName", result.FileName);
            Assert.Equal("file/type", result.MimeType);
            Assert.Equal(0, result.FileSize);
        }
    }

    [Fact]
    public async Task Should_be_serializable_to_json()
    {
        await using (var source = new TempAssetFile("fileName", "file/type"))
        {
            var deserialized = JsonConvert.DeserializeObject<TempAssetFile>(JsonConvert.SerializeObject(source));

            Assert.Equal(source.FileName, deserialized?.FileName);
        }
    }

    [Fact]
    public async Task Should_allow_multiple_reads_and_writes()
    {
        await using (var result = new TempAssetFile("fileName", "file/type"))
        {
            var buffer = new byte[] { 1, 2, 3, 4 };

            await using (var stream = result.OpenWrite())
            {
                await stream.WriteAsync(buffer.AsMemory());
            }

            for (var i = 0; i < 3; i++)
            {
                await using (var stream = result.OpenWrite())
                {
                    var read = new byte[4];

                    var bytesRead = await stream.ReadAsync(read.AsMemory());

                    Assert.Equal(buffer.Length, bytesRead);
                    Assert.Equal(buffer, read);
                }
            }
        }
    }
}
