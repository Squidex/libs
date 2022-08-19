// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;

namespace Squidex.Messaging
{
    public sealed class TransportHeaders : Dictionary<string, string>
    {
        private const string ISO8601Format = "yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz";

        public TransportHeaders Set(string key, string value)
        {
            this[key] = value;
            return this;
        }

        public TransportHeaders Set(string key, Guid value)
        {
            this[key] = value.ToString();
            return this;
        }

        public TransportHeaders Set(string key, TimeSpan value)
        {
            this[key] = value.ToString();
            return this;
        }

        public TransportHeaders Set(string key, DateTime value)
        {
            this[key] = value.ToString(ISO8601Format, CultureInfo.InvariantCulture);
            return this;
        }

        public bool TryGetTimestamp(string key, out TimeSpan result)
        {
            result = default;

            if (TryGetValue(key, out var value) && TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }

            return false;
        }

        public bool TryGetDateTime(string key, out DateTime result)
        {
            result = default;

            if (TryGetValue(key, out var value) && DateTime.TryParseExact(value, ISO8601Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return true;
            }

            return false;
        }
    }
}
