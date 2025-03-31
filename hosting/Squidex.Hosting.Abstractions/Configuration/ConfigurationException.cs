// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;

namespace Squidex.Hosting.Configuration;

[Serializable]
public class ConfigurationException(IReadOnlyList<ConfigurationError> errors, Exception? inner = null) : Exception(FormatMessage(errors), inner)
{
    public IReadOnlyList<ConfigurationError> Errors { get; } = errors;

    public ConfigurationException(ConfigurationError error, Exception? inner = null)
        : this([error], inner)
    {
    }

    private static string FormatMessage(IReadOnlyList<ConfigurationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var sb = new StringBuilder();

        foreach (var error in errors)
        {
            sb.AppendLine(error.ToString());
        }

        return sb.ToString();
    }
}
