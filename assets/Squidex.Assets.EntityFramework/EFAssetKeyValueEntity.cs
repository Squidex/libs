// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Squidex.Assets.EntityFramework;

public sealed class EFAssetKeyValueEntity<T> where T : class
{
    [Key]
    public string Key { get; set; }

    public string Value { get; set; }

    public DateTimeOffset Expires { get; set; }

    public static EFAssetKeyValueEntity<T> Create(string key, T value, DateTimeOffset expires,
        JsonSerializerOptions options)
    {
        var serialized = JsonSerializer.Serialize(value, options);

        return new EFAssetKeyValueEntity<T> { Key = key, Value = serialized, Expires = expires.ToUniversalTime() };
    }

    public void Update(T value, JsonSerializerOptions options)
    {
        Value = JsonSerializer.Serialize(value, options);
    }

    public T GetValue(JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(Value, options) ??
            throw new JsonException("Failed to deserialize json.");
    }
}
