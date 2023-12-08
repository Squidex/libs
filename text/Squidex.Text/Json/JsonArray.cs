// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Squidex.Text;

public sealed class JsonArray : List<JsonValue>, IEquatable<JsonArray>
{
    public JsonArray()
    {
    }

    public JsonArray(int capacity)
        : base(capacity)
    {
    }

    public JsonArray(JsonArray source)
        : base(source)
    {
    }

    public JsonArray(IEnumerable<JsonValue>? source)
    {
        if (source != null)
        {
            foreach (var item in source)
            {
                Add(item);
            }
        }
    }

    public new JsonArray Add(JsonValue value)
    {
        base.Add(value);

        return this;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as JsonArray);
    }

    public bool Equals(JsonArray? array)
    {
        if (array == null)
        {
            return false;
        }

        if (ReferenceEquals(this, array))
        {
            return true;
        }

        if (Count != array.Count)
        {
            return false;
        }

        for (var i = 0; i < Count; i++)
        {
            if (!this[i].Equals(array[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = 17;

        foreach (var item in this)
        {
            if (!Equals(item, null))
            {
                hashCode = (hashCode * 23) + EqualityComparer<JsonValue>.Default.GetHashCode(item);
            }
        }

        return hashCode;
    }

    public override string ToString()
    {
        return $"[{string.Join(", ", this.Select(x => x.ToJsonString()))}]";
    }

    public bool TryGetValue(string pathSegment, [MaybeNullWhen(false)] out JsonValue result)
    {
        ArgumentNullException.ThrowIfNull(pathSegment);

        result = default;

        if (pathSegment != null && int.TryParse(pathSegment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) && index >= 0 && index < Count)
        {
            result = this[index];

            return true;
        }

        return false;
    }
}
