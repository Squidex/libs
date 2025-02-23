// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.IO.Compression;
using Xunit;

namespace Squidex.Assets;

public abstract class AssetStoreTests
{
    private readonly MemoryStream assetLarge = CreateFile(4 * 1024 * 1024);
    private readonly MemoryStream assetSmall = CreateFile(4);

    protected string FileName { get; } = Guid.NewGuid().ToString();

    protected virtual bool CanUploadStreamsWithoutLength => true;

    protected virtual bool CanDeleteAssetsWithPrefix => true;

    public abstract Task<IAssetStore> CreateSutAsync();

    public enum TestCase
    {
        NoFolder,
        FolderWindows,
        FolderLinux,
    }

    public static readonly TheoryData<TestCase> FolderCases =
    [
        TestCase.NoFolder,
        TestCase.FolderWindows,
        TestCase.FolderLinux,
    ];

    [Theory]
    [InlineData("../{file}.png")]
    [InlineData("../../{file}.png")]
    [InlineData("./../../{file}.png")]
    [InlineData("folder/../../{file}.png")]
    public async Task Should_not_be_able_to_store_files_in_parent_folder(string path)
    {
        var sut = await CreateSutAsync();

        path = path.Replace("{file}", Guid.NewGuid().ToString(), StringComparison.Ordinal);

        var data = new MemoryStream([0x1, 0x2, 0x3, 0x4, 0x5]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UploadAsync(path, data, true));
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public virtual async Task Should_throw_exception_if_asset_to_get_size_is_not_found(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        await Assert.ThrowsAsync<AssetNotFoundException>(() => sut.GetSizeAsync(path));
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public virtual async Task Should_throw_exception_if_asset_to_download_is_not_found(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        await Assert.ThrowsAsync<AssetNotFoundException>(() => sut.DownloadAsync(path, new MemoryStream()));
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_throw_exception_if_asset_to_copy_is_not_found(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        await Assert.ThrowsAsync<AssetNotFoundException>(() => sut.CopyAsync(path, Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task Should_throw_exception_if_stream_to_download_is_null()
    {
        var sut = await CreateSutAsync();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.DownloadAsync("File", null!));
    }

    [Fact]
    public async Task Should_throw_exception_if_stream_to_upload_is_null()
    {
        var sut = await CreateSutAsync();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UploadAsync("File", null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Should_throw_exception_if_source_file_name_to_copy_is_empty(string? input)
    {
        var sut = await CreateSutAsync();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.CopyAsync(input!, "Target"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Should_throw_exception_if_target_file_name_to_copy_is_empty(string? input)
    {
        var sut = await CreateSutAsync();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.CopyAsync("Source", input!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Should_throw_exception_if_file_name_to_delete_is_empty(string? input)
    {
        var sut = await CreateSutAsync();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.DeleteAsync(input!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Should_throw_exception_if_file_name_to_download_is_empty(string? input)
    {
        var sut = await CreateSutAsync();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.DownloadAsync(input!, new MemoryStream()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Should_throw_exception_if_file_name_to_upload_is_empty(string? input)
    {
        var sut = await CreateSutAsync();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.UploadAsync(input!, new MemoryStream()));
    }

    [Fact]
    public async Task Should_unify_folders2()
    {
        var sut = await CreateSutAsync();

        var folder = Guid.NewGuid().ToString();
        await sut.UploadAsync(GetPath(TestCase.FolderLinux, folder, folder), assetSmall);

        var readData = new MemoryStream();
        await sut.DownloadAsync(GetPath(TestCase.FolderWindows, folder, folder), readData);

        Assert.Equal(assetSmall.ToArray(), readData.ToArray());
    }

    [Fact]
    public async Task Should_unify_folders1()
    {
        var sut = await CreateSutAsync();

        var folder = Guid.NewGuid().ToString();
        await sut.UploadAsync(GetPath(TestCase.FolderLinux, folder, folder), assetSmall);

        var readData = new MemoryStream();
        await sut.DownloadAsync(GetPath(TestCase.FolderWindows, folder, folder), readData);

        Assert.Equal(assetSmall.ToArray(), readData.ToArray());
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_upload_compressed_file(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        if (!CanUploadStreamsWithoutLength)
        {
            return;
        }

        var source = CreateDeflateStream(20_000);
        await sut.UploadAsync(path, source);

        var readData = new MemoryStream();
        await sut.DownloadAsync(path, readData);

        Assert.True(readData.Length > 0);
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_write_and_read_file(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        await sut.UploadAsync(path, assetSmall);

        var readData = new MemoryStream();
        await sut.DownloadAsync(path, readData);

        Assert.Equal(assetSmall.ToArray(), readData.ToArray());
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_write_and_read_large_file(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        await sut.UploadAsync(path, assetLarge);

        var readData = new MemoryStream();
        await sut.DownloadAsync(path, readData);

        Assert.Equal(assetLarge.ToArray(), readData.ToArray());
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_write_and_read_file_with_range(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        await sut.UploadAsync(path, assetLarge, true);

        var readData = new MemoryStream();
        await sut.DownloadAsync(path, readData, new BytesRange(2, 5));

        Assert.Equal(assetLarge.ToArray().AsSpan().Slice(2, 4).ToArray(), readData.ToArray());
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_copy_and_read_file(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        var tempFile = Guid.NewGuid().ToString();

        await sut.UploadAsync(tempFile, assetSmall);
        try
        {
            await sut.CopyAsync(tempFile, path);

            var readData = new MemoryStream();
            await sut.DownloadAsync(path, readData);

            Assert.Equal(assetSmall.ToArray(), readData.ToArray());
        }
        finally
        {
            await sut.DeleteAsync(tempFile);
        }
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_write_and_and_get_size(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        await sut.UploadAsync(path, assetSmall, true);

        var size = await sut.GetSizeAsync(path);

        Assert.Equal(assetSmall.Length, size);
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_write_and_read_file_and_overwrite_non_existing(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        await sut.UploadAsync(path, assetSmall, true);

        var readData = new MemoryStream();
        await sut.DownloadAsync(path, readData);

        Assert.Equal(assetSmall.ToArray(), readData.ToArray());
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_write_and_read_overrided_file(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        var oldData = new MemoryStream([0x1, 0x2, 0x3, 0x4, 0x5]);

        await sut.UploadAsync(path, oldData);
        await sut.UploadAsync(path, assetSmall, true);

        var readData = new MemoryStream();
        await sut.DownloadAsync(path, readData);

        Assert.Equal(assetSmall.ToArray(), readData.ToArray());
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_throw_exception_when_file_to_write_already_exists(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        await sut.UploadAndResetAsync(path, assetSmall);

        await Assert.ThrowsAsync<AssetAlreadyExistsException>(() => sut.UploadAsync(path, assetSmall));
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_throw_exception_when_target_file_to_copy_to_already_exists(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        var tempFile = Guid.NewGuid().ToString();

        await sut.UploadAsync(tempFile, assetSmall);
        await sut.CopyAsync(tempFile, path);

        await Assert.ThrowsAsync<AssetAlreadyExistsException>(() => sut.CopyAsync(tempFile, path));
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_ignore_when_deleting_deleted_file(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        await sut.UploadAsync(path, assetSmall);
        await sut.DeleteAsync(path);
        await sut.DeleteAsync(path);
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_ignore_when_deleting_not_existing_file(TestCase testCase)
    {
        var sut = await CreateSutAsync();

        var path = GetPath(testCase);

        await sut.DeleteAsync(path);
    }

    [Fact]
    public async Task Should_delete_by_prefix_name_if_asset_exists()
    {
        var sut = await CreateSutAsync();

        var name = Guid.NewGuid().ToString();

        await sut.UploadAndResetAsync(name, assetSmall);
        await sut.DeleteByPrefixAsync(name);
    }

    [Fact]
    public async Task Should_delete_folder_prefix()
    {
        var sut = await CreateSutAsync();

        var folder1 = Guid.NewGuid().ToString();
        var folder2 = Guid.NewGuid().ToString();

        await sut.UploadAndResetAsync($"{folder1}/file1.txt", assetSmall);
        await sut.UploadAndResetAsync($"{folder1}/file2.txt", assetSmall);

        await sut.UploadAndResetAsync($"{folder2}/file1.txt", assetSmall);
        await sut.UploadAndResetAsync($"{folder2}/file2.txt", assetSmall);

        await sut.DeleteByPrefixAsync(folder1);

        await Assert.ThrowsAsync<AssetNotFoundException>(() => sut.GetSizeAsync($"{folder1}/file1.txt"));
        await Assert.ThrowsAsync<AssetNotFoundException>(() => sut.GetSizeAsync($"{folder1}/file2.txt"));

        Assert.True(await sut.GetSizeAsync($"{folder2}/file1.txt") > 0);
        Assert.True(await sut.GetSizeAsync($"{folder2}/file2.txt") > 0);
    }

    [Fact]
    public async Task Should_delete_normal_prefix()
    {
        if (!CanDeleteAssetsWithPrefix)
        {
            return;
        }

        var sut = await CreateSutAsync();

        var folder1 = Guid.NewGuid().ToString();
        var folder2 = Guid.NewGuid().ToString();

        await sut.UploadAndResetAsync($"{folder1}/prefix1-file1.txt", assetSmall);
        await sut.UploadAndResetAsync($"{folder1}/prefix1-file2.txt", assetSmall);

        await sut.UploadAndResetAsync($"{folder2}/prefix2-file1.txt", assetSmall);
        await sut.UploadAndResetAsync($"{folder2}/prefix2-file2.txt", assetSmall);

        await sut.DeleteByPrefixAsync($"{folder1}/prefix1");

        await Assert.ThrowsAsync<AssetNotFoundException>(() => sut.GetSizeAsync($"{folder1}/prefix1-file1.txt"));
        await Assert.ThrowsAsync<AssetNotFoundException>(() => sut.GetSizeAsync($"{folder1}/prefix1-file2.txt"));

        Assert.True(await sut.GetSizeAsync($"{folder2}/prefix2-file1.txt") > 0);
        Assert.True(await sut.GetSizeAsync($"{folder2}/prefix2-file2.txt") > 0);
    }

    private static MemoryStream CreateFile(int length)
    {
        var memoryStream = new MemoryStream();

        for (var i = 0; i < length; i++)
        {
            memoryStream.WriteByte((byte)i);
        }

        memoryStream.Position = 0;

        return memoryStream;
    }

    private static Stream CreateDeflateStream(int length)
    {
        var memoryStream = new MemoryStream();

        using (var archive1 = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            using (var file = archive1.CreateEntry("test").Open())
            {
                var test = CreateFile(length);

                test.CopyTo(file);
            }
        }

        memoryStream.Position = 0;

        var archive2 = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        return archive2.GetEntry("test")!.Open();
    }

    private static string GetPath(TestCase testCase, string? folder = null, string? file = null)
    {
        file ??= Guid.NewGuid().ToString();

        switch (testCase)
        {
            case TestCase.FolderWindows:
                return $"{folder ?? Guid.NewGuid().ToString()}\\{file}";
            case TestCase.FolderLinux:
                return $"{folder ?? Guid.NewGuid().ToString()}/{file}";
            default:
                return $"{Guid.NewGuid()}";
        }
    }
}
