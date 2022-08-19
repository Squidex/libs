// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using System.Text;

namespace Squidex.Assets
{
    public sealed class BlurOptions : IOptions
    {
        public int ComponentX { get; set; } = 4;

        public int ComponentY { get; set; } = 4;

        public IEnumerable<KeyValuePair<string, string>>? ExtraParameters { get; set; }

        public IEnumerable<(string, string)> ToParameters()
        {
            if (ComponentX != default)
            {
                yield return ("componentX", ComponentX.ToString(CultureInfo.InvariantCulture)!);
            }

            if (ComponentX != default)
            {
                yield return ("componentY", ComponentY.ToString(CultureInfo.InvariantCulture)!);
            }

            if (ExtraParameters != null)
            {
                foreach (var kvp in ExtraParameters)
                {
                    yield return (kvp.Key, kvp.Value);
                }
            }
        }

        public static BlurOptions Parse(Dictionary<string, string> parameters)
        {
            var result = new BlurOptions();

            bool TryParseInt(string key, out int value)
            {
                value = 0;

                return parameters.TryGetValue(key, out var temp) && int.TryParse(temp, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
            }

            if (TryParseInt("componentX", out var componentX))
            {
                result.ComponentX = componentX;
            }

            if (TryParseInt("componentY", out var componentY))
            {
                result.ComponentY = componentY;
            }

            return result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(20);

            sb.Append(ComponentX);
            sb.Append('_');
            sb.Append(ComponentY);

            return sb.ToString();
        }
    }
}
