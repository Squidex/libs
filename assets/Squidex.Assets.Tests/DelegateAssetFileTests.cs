// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Newtonsoft.Json;
using Xunit;

namespace Squidex.Assets
{
    public class DelegateAssetFileTests
    {
        [Fact]
        public void Should_be_serializable_to_json()
        {
            var source = new DelegateAssetFile("fileName", "file/type", 1024, () => new MemoryStream());

            var deserialized = JsonConvert.DeserializeObject<DelegateAssetFile>(JsonConvert.SerializeObject(source));

            Assert.Equal(source.FileName, deserialized?.FileName);
        }
    }
}
