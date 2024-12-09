// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace Squidex.Flows.Steps.Utils;

public static class HashExtensions
{
    private const int MaxStackSize = 128;
    private delegate int HashMethod(ReadOnlySpan<byte> source, Span<byte> destination);
    private delegate string EncodeMethod(ReadOnlySpan<byte> source);
    private static readonly HashMethod HashMD5 = MD5.HashData;
    private static readonly HashMethod HashSHA256 = SHA256.HashData;
    private static readonly HashMethod HashSHA512 = SHA512.HashData;
    private static readonly EncodeMethod EncodeUTF8 = Encoding.UTF8.GetString;
    private static readonly EncodeMethod EncodeBase64 = input => Convert.ToBase64String(input, default);

    public static string ToSha256Base64(this string value)
    {
        return ToHashed(value, HashSHA256, SHA256.HashSizeInBits, Encoding.UTF8, EncodeBase64);
    }

    private static string ToHashed(this string value, HashMethod algorithm, int hashSize, Encoding encoding, EncodeMethod encode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string result;

        var length = encoding.GetByteCount(value);

        if (length > MaxStackSize)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                result = ConvertCore(algorithm, hashSize, value.AsSpan(), buffer, encode, encoding);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        else
        {
            Span<byte> buffer = stackalloc byte[length];

            result = ConvertCore(algorithm, hashSize, value.AsSpan(), buffer, encode, encoding);
        }

        static string ConvertCore(HashMethod algorithm, int hashSize, ReadOnlySpan<char> source, Span<byte> destination, EncodeMethod encode, Encoding encoding)
        {
            var written = encoding.GetBytes(source, destination);

            return ToHashed(destination[..written], algorithm, hashSize, encode);
        }

        return result;
    }

    private static string ToHashed(ReadOnlySpan<byte> bytes, HashMethod algorithm, int hashSize, EncodeMethod encode)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        string result;

        var length = hashSize / 8;

        if (length > MaxStackSize)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                result = ConvertCore(algorithm, bytes, buffer, encode);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        else
        {
            Span<byte> buffer = stackalloc byte[length];

            return ConvertCore(algorithm, bytes, buffer, encode);
        }

        return result;

        static string ConvertCore(HashMethod algorithm, ReadOnlySpan<byte> source, Span<byte> destination, EncodeMethod encode)
        {
            var written = algorithm(source, destination);

            return encode(destination[..written]);
        }
    }
}
