// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.Serialization;
using Squidex.Assets.Internal;

namespace Squidex.Assets
{
    [Serializable]
    public class AssetNotFoundException : Exception
    {
        public AssetNotFoundException(string fileName, Exception? inner = null)
            : base(FormatMessage(fileName), inner)
        {
        }

        protected AssetNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        private static string FormatMessage(string fileName)
        {
            Guard.NotNullOrEmpty(fileName, nameof(fileName));

            return $"An asset with name '{fileName}' does not exist.";
        }
    }
}
