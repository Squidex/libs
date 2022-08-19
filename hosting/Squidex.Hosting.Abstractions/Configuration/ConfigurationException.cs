// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.Serialization;
using System.Text;

namespace Squidex.Hosting.Configuration
{
    [Serializable]
    public class ConfigurationException : Exception
    {
        public IReadOnlyList<ConfigurationError> Errors { get; }

        public ConfigurationException(ConfigurationError error, Exception? inner = null)
            : this(new List<ConfigurationError> { error }, inner)
        {
        }

        public ConfigurationException(IReadOnlyList<ConfigurationError> errors, Exception? inner = null)
            : base(FormatMessage(errors), inner)
        {
            Errors = errors;
        }

        protected ConfigurationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Errors = (List<ConfigurationError>)info.GetValue(nameof(Errors), typeof(List<ConfigurationError>))!;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Errors), Errors);

            base.GetObjectData(info, context);
        }

        private static string FormatMessage(IReadOnlyList<ConfigurationError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            var sb = new StringBuilder();

            foreach (var error in errors)
            {
                sb.AppendLine(error.ToString());
            }

            return sb.ToString();
        }
    }
}
