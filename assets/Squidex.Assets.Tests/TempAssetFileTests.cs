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
    public void Should_construct()
    {
        using (var result = new TempAssetFile("fileName", "file/type", 1024))
        {
            Assert.Equal("fileName", result.FileName);
            Assert.Equal("file/type", result.MimeType);
            Assert.Equal(1024, result.FileSize);
        }
    }

    [Fact]
    public void Should_construct_from_other_file()
    {
        var source = new DelegateAssetFile("fileName", "file/type", 1024, () => new MemoryStream());

        using (var result = TempAssetFile.Create(source))
        {
            Assert.Equal("fileName", result.FileName);
            Assert.Equal("file/type", result.MimeType);
            Assert.Equal(1024, result.FileSize);
        }
    }

    [Fact]
    public void Should_be_serializable_to_json()
    {
        var source = new TempAssetFile("fileName", "file/type", 1024);

        var deserialized = JsonConvert.DeserializeObject<TempAssetFile>(JsonConvert.SerializeObject(source));

        Assert.Equal(source.FileName, deserialized?.FileName);
    }

    [Fact]
    public void Should_allow_multiple_reads_and_writes()
    {
        using (var result = new TempAssetFile("fileName", "file/type", 1024))
        {
            var buffer = new byte[] { 1, 2, 3, 4 };

            using (var stream = result.OpenWrite())
            {
                stream.Write(buffer, 0, buffer.Length);
            }

            for (var i = 0; i < 3; i++)
            {
                using (var stream = result.OpenWrite())
                {
                    var read = new byte[4];

                    var bytesRead = stream.Read(read, 0, read.Length);

                    Assert.Equal(buffer.Length, bytesRead);
                    Assert.Equal(buffer, read);
                }
            }
        }
    }
}
