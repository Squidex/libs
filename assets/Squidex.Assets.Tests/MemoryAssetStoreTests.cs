// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Assets
{
    public class MemoryAssetStoreTests : AssetStoreTests<MemoryAssetStore>
    {
        public override MemoryAssetStore CreateStore()
        {
            return new MemoryAssetStore();
        }

        [Fact]
        public void Should_not_calculate_source_url()
        {
            var url = ((IAssetStore)Sut).GeneratePublicUrl(FileName);

            Assert.Null(url);
        }
    }
}