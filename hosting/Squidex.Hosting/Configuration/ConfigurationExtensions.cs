﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using Squidex.Hosting.Configuration;

namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationExtensions
    {
        public static T GetOptionalValue<T>(this IConfiguration config, string path, T? defaultValue = default)
        {
            var value = config.GetValue(path, defaultValue!);

            return value;
        }

        public static int GetOptionalValue(this IConfiguration config, string path, int defaultValue)
        {
            var value = config.GetValue<string>(path);

            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                result = defaultValue;
            }

            return result;
        }

        public static string GetRequiredValue(this IConfiguration config, string path)
        {
            var value = config.GetValue<string>(path);

            if (string.IsNullOrWhiteSpace(value))
            {
                var error = new ConfigurationError("Value is required.", path);

                throw new ConfigurationException(error);
            }

            return value;
        }

        public static string ConfigureByOption(this IConfiguration config, string path, Alternatives options)
        {
            var value = config.GetRequiredValue(path);

            if (options.TryGetValue(value, out var action))
            {
                action();
            }
            else
            {
                var error = new ConfigurationError($"Unsupported value '{value}', supported: {string.Join(" ", options.Keys)}.", path);

                throw new ConfigurationException(error);
            }

            return value;
        }
    }
}
