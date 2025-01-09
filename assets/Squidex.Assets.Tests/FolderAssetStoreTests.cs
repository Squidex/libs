// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FakeItEasy;
using Microsoft.Extensions.Options;
using Xunit;

namespace Squidex.Assets;

public class FolderAssetStoreTests(FolderAssetStoreFixture fixture)
    : AssetStoreTests, IClassFixture<FolderAssetStoreFixture>
{
    public override Task<IAssetStore> CreateSutAsync()
    {
        return Task.FromResult<IAssetStore>(fixture.Store);
    }

    [Fact]
    public async Task Should_throw_when_creating_directory_failed()
    {
        var options = Options.Create(new FolderAssetOptions
        {
            Path = CreateInvalidPath()
        });

        await Assert.ThrowsAsync<AssetStoreException>(() => new FolderAssetStore(options, A.Dummy<ILogger<FolderAssetStore>>()).InitializeAsync(default));
    }

    [Fact]
    public void Should_create_directory_when_connecting()
    {
        Assert.True(Directory.Exists(fixture.TestFolder));
    }

    [Fact]
    public void Should_calculate_source_url()
    {
        var url = ((IAssetStore)fixture.Store).GeneratePublicUrl(FileName);

        Assert.Null(url);
    }

    private static string CreateInvalidPath()
    {
        var windir = Environment.GetEnvironmentVariable("windir");

        return !string.IsNullOrWhiteSpace(windir) ? "Z://invalid" : "/proc/invalid";
    }
}
