// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.IO.Compression;
using Xunit;

#pragma warning disable RECS0108 // Warns about static fields in generic types

namespace Squidex.Assets;

public abstract class AssetStoreTests<T> where T : IAssetStore
{
    private readonly MemoryStream assetLarge = CreateFile(4 * 1024 * 1024);
    private readonly MemoryStream assetSmall = CreateFile(4);
    private readonly Lazy<T> sut;

    protected T Sut
    {
        get { return sut.Value; }
    }

    protected string FileName { get; } = Guid.NewGuid().ToString();

    protected virtual bool CanUploadStreamsWithoutLength => true;

    protected virtual bool CanDeleteAssetsWithPrefix => true;

    protected AssetStoreTests()
    {
        sut = new Lazy<T>(CreateStore);
    }

    public abstract T CreateStore();

    public enum TestCase
    {
        NoFolder,
        FolderWindows,
        FolderLinux
    }

    public static readonly TheoryData<TestCase> FolderCases = new TheoryData<TestCase>
    {
        TestCase.NoFolder,
        TestCase.FolderWindows,
        TestCase.FolderLinux
    };

    [Theory]
    [InlineData("../{file}.png")]
    [InlineData("../../{file}.png")]
    [InlineData("./../../{file}.png")]
    [InlineData("folder/../../{file}.png")]
    public async Task Should_not_be_able_to_store_files_in_parent_folder(string path)
    {
        path = path.Replace("{file}", Guid.NewGuid().ToString(), StringComparison.Ordinal);

        var data = new MemoryStream([0x1, 0x2, 0x3, 0x4, 0x5]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut.UploadAsync(path, data, true));
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public virtual async Task Should_throw_exception_if_asset_to_get_size_is_not_found(TestCase testCase)
    {
        var path = GetPath(testCase);

        await Assert.ThrowsAsync<AssetNotFoundException>(() => Sut.GetSizeAsync(path));
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public virtual async Task Should_throw_exception_if_asset_to_download_is_not_found(TestCase testCase)
    {
        var path = GetPath(testCase);

        await Assert.ThrowsAsync<AssetNotFoundException>(() => Sut.DownloadAsync(path, new MemoryStream()));
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_throw_exception_if_asset_to_copy_is_not_found(TestCase testCase)
    {
        var path = GetPath(testCase);

        await Assert.ThrowsAsync<AssetNotFoundException>(() => Sut.CopyAsync(path, Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task Should_throw_exception_if_stream_to_download_is_null()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Sut.DownloadAsync("File", null!));
    }

    [Fact]
    public async Task Should_throw_exception_if_stream_to_upload_is_null()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Sut.UploadAsync("File", null!));
    }

    [Fact]
    public async Task Should_throw_exception_if_source_file_name_to_copy_is_empty()
    {
        await CheckEmpty(v => Sut.CopyAsync(v, "Target"));
    }

    [Fact]
    public async Task Should_throw_exception_if_target_file_name_to_copy_is_empty()
    {
        await CheckEmpty(v => Sut.CopyAsync("Source", v));
    }

    [Fact]
    public async Task Should_throw_exception_if_file_name_to_delete_is_empty()
    {
        await CheckEmpty(v => Sut.DeleteAsync(v));
    }

    [Fact]
    public async Task Should_throw_exception_if_file_name_to_download_is_empty()
    {
        await CheckEmpty(v => Sut.DownloadAsync(v, new MemoryStream()));
    }

    [Fact]
    public async Task Should_throw_exception_if_file_name_to_upload_is_empty()
    {
        await CheckEmpty(v => Sut.UploadAsync(v, new MemoryStream()));
    }

    [Fact]
    public async Task Should_unify_folders2()
    {
        var folder = Guid.NewGuid().ToString();

        await Sut.UploadAsync(GetPath(TestCase.FolderLinux, folder, folder), assetSmall);

        var readData = new MemoryStream();

        await Sut.DownloadAsync(GetPath(TestCase.FolderWindows, folder, folder), readData);

        Assert.Equal(assetSmall.ToArray(), readData.ToArray());
    }

    [Fact]
    public async Task Should_unify_folders1()
    {
        var folder = Guid.NewGuid().ToString();

        await Sut.UploadAsync(GetPath(TestCase.FolderLinux, folder, folder), assetSmall);

        var readData = new MemoryStream();

        await Sut.DownloadAsync(GetPath(TestCase.FolderWindows, folder, folder), readData);

        Assert.Equal(assetSmall.ToArray(), readData.ToArray());
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_upload_compressed_file(TestCase testCase)
    {
        var path = GetPath(testCase);

        if (!CanUploadStreamsWithoutLength)
        {
            return;
        }

        var source = CreateDeflateStream(20_000);

        await Sut.UploadAsync(path, source);

        var readData = new MemoryStream();

        await Sut.DownloadAsync(path, readData);

        Assert.True(readData.Length > 0);
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_write_and_read_file(TestCase testCase)
    {
        var path = GetPath(testCase);

        await Sut.UploadAsync(path, assetSmall);

        var readData = new MemoryStream();

        await Sut.DownloadAsync(path, readData);

        Assert.Equal(assetSmall.ToArray(), readData.ToArray());
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_write_and_read_large_file(TestCase testCase)
    {
        var path = GetPath(testCase);

        await Sut.UploadAsync(path, assetLarge);

        var readData = new MemoryStream();

        await Sut.DownloadAsync(path, readData);

        Assert.Equal(assetLarge.ToArray(), readData.ToArray());
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_write_and_read_file_with_range(TestCase testCase)
    {
        var path = GetPath(testCase);

        await Sut.UploadAsync(path, assetSmall, true);

        var readData = new MemoryStream();

        await Sut.DownloadAsync(path, readData, new BytesRange(1, 2));

        Assert.Equal(new Span<byte>(assetSmall.ToArray()).Slice(1, 2).ToArray(), readData.ToArray());
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_copy_and_read_file(TestCase testCase)
    {
        var path = GetPath(testCase);

        var tempFile = Guid.NewGuid().ToString();

        await Sut.UploadAsync(tempFile, assetSmall);
        try
        {
            await Sut.CopyAsync(tempFile, path);

            var readData = new MemoryStream();

            await Sut.DownloadAsync(path, readData);

            Assert.Equal(assetSmall.ToArray(), readData.ToArray());
        }
        finally
        {
            await Sut.DeleteAsync(tempFile);
        }
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_write_and_and_get_size(TestCase testCase)
    {
        var path = GetPath(testCase);

        await Sut.UploadAsync(path, assetSmall, true);

        var size = await Sut.GetSizeAsync(path);

        Assert.Equal(assetSmall.Length, size);
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_write_and_read_file_and_overwrite_non_existing(TestCase testCase)
    {
        var path = GetPath(testCase);

        await Sut.UploadAsync(path, assetSmall, true);

        var readData = new MemoryStream();

        await Sut.DownloadAsync(path, readData);

        Assert.Equal(assetSmall.ToArray(), readData.ToArray());
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_write_and_read_overrided_file(TestCase testCase)
    {
        var path = GetPath(testCase);

        var oldData = new MemoryStream([0x1, 0x2, 0x3, 0x4, 0x5]);

        await Sut.UploadAsync(path, oldData);
        await Sut.UploadAsync(path, assetSmall, true);

        var readData = new MemoryStream();

        await Sut.DownloadAsync(path, readData);

        Assert.Equal(assetSmall.ToArray(), readData.ToArray());
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_throw_exception_when_file_to_write_already_exists(TestCase testCase)
    {
        var path = GetPath(testCase);

        await Sut.UploadAndResetAsync(path, assetSmall);

        await Assert.ThrowsAsync<AssetAlreadyExistsException>(() => Sut.UploadAsync(path, assetSmall));
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_throw_exception_when_target_file_to_copy_to_already_exists(TestCase testCase)
    {
        var path = GetPath(testCase);

        var tempFile = Guid.NewGuid().ToString();

        await Sut.UploadAsync(tempFile, assetSmall);
        await Sut.CopyAsync(tempFile, path);

        await Assert.ThrowsAsync<AssetAlreadyExistsException>(() => Sut.CopyAsync(tempFile, path));
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_ignore_when_deleting_deleted_file(TestCase testCase)
    {
        var path = GetPath(testCase);

        await Sut.UploadAsync(path, assetSmall);
        await Sut.DeleteAsync(path);
        await Sut.DeleteAsync(path);
    }

    [Theory]
    [MemberData(nameof(FolderCases))]
    public async Task Should_ignore_when_deleting_not_existing_file(TestCase testCase)
    {
        var path = GetPath(testCase);

        await Sut.DeleteAsync(path);
    }

    [Fact]
    public async Task Should_delete_by_prefix_name_if_asset_exists()
    {
        var name = Guid.NewGuid().ToString();

        await Sut.UploadAndResetAsync(name, assetSmall);
        await Sut.DeleteByPrefixAsync(name);
    }

    [Fact]
    public async Task Should_delete_folder_prefix()
    {
        var folder1 = Guid.NewGuid().ToString();
        var folder2 = Guid.NewGuid().ToString();

        await Sut.UploadAndResetAsync($"{folder1}/file1.txt", assetSmall);
        await Sut.UploadAndResetAsync($"{folder1}/file2.txt", assetSmall);

        await Sut.UploadAndResetAsync($"{folder2}/file1.txt", assetSmall);
        await Sut.UploadAndResetAsync($"{folder2}/file2.txt", assetSmall);

        await Sut.DeleteByPrefixAsync(folder1);

        await Assert.ThrowsAsync<AssetNotFoundException>(() => Sut.GetSizeAsync($"{folder1}/file1.txt"));
        await Assert.ThrowsAsync<AssetNotFoundException>(() => Sut.GetSizeAsync($"{folder1}/file2.txt"));

        Assert.True(await Sut.GetSizeAsync($"{folder2}/file1.txt") > 0);
        Assert.True(await Sut.GetSizeAsync($"{folder2}/file2.txt") > 0);
    }

    [Fact]
    public async Task Should_delete_normal_prefix()
    {
        if (!CanDeleteAssetsWithPrefix)
        {
            return;
        }

        var folder1 = Guid.NewGuid().ToString();
        var folder2 = Guid.NewGuid().ToString();

        await Sut.UploadAndResetAsync($"{folder1}/prefix1-file1.txt", assetSmall);
        await Sut.UploadAndResetAsync($"{folder1}/prefix1-file2.txt", assetSmall);

        await Sut.UploadAndResetAsync($"{folder2}/prefix2-file1.txt", assetSmall);
        await Sut.UploadAndResetAsync($"{folder2}/prefix2-file2.txt", assetSmall);

        await Sut.DeleteByPrefixAsync($"{folder1}/prefix1");

        await Assert.ThrowsAsync<AssetNotFoundException>(() => Sut.GetSizeAsync($"{folder1}/prefix1-file1.txt"));
        await Assert.ThrowsAsync<AssetNotFoundException>(() => Sut.GetSizeAsync($"{folder1}/prefix1-file2.txt"));

        Assert.True(await Sut.GetSizeAsync($"{folder2}/prefix2-file1.txt") > 0);
        Assert.True(await Sut.GetSizeAsync($"{folder2}/prefix2-file2.txt") > 0);
    }

    private static async Task CheckEmpty(Func<string, Task> action)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => action(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => action(string.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => action(" "));
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
