// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Security.Cryptography;

namespace Squidex.Assets;

public class HasherStreamTests
{
    [Fact]
    public void Should_calculate_hash_while_copying()
    {
        var source = GenerateTestData();
        var sourceHash = Sha256Base64(source);

        var sourceStream = new HasherStream(new MemoryStream(source), HashAlgorithmName.SHA256);

        using (sourceStream)
        {
            var target = new MemoryStream();

            sourceStream.CopyTo(target);

            var targetHash = sourceStream.GetHashStringAndReset();

            Assert.Equal(sourceHash, targetHash);
        }
    }

    private static byte[] GenerateTestData(int length = 1000)
    {
        var random = new Random();
        var result = new byte[length];

        random.NextBytes(result);

        return result;
    }

    private static string Sha256Base64(byte[] bytes)
    {
        var bytesHash = SHA256.HashData(bytes);

        var result = Convert.ToBase64String(bytesHash);

        return result;
    }
}
