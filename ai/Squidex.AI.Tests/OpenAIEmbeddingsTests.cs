// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Squidex.Hosting;
using TestHelpers;

namespace Squidex.AI;

public class OpenAIEmbeddingsTests
{
    [Fact]
    [Trait("Category", "Dependencies")]
    public async Task Should_calculate_vector()
    {
        var (sut, _) = await CreateSutAsync();

        var vector = await sut.CalculateEmbeddingsAsync("What is Squidex?", default);

        Assert.Equal(3072, vector.Length);
    }

    private static async Task<(IEmbeddings, IServiceProvider)> CreateSutAsync()
    {
        var services =
            new ServiceCollection()
                .AddAI()
                .AddOpenAIEmbeddings(TestUtils.Configuration)
                .Services
                .BuildServiceProvider();

        var initializables = services.GetRequiredService<IEnumerable<IInitializable>>();

        foreach (var initializable in initializables)
        {
            await initializable.InitializeAsync(default);
        }

        return (services.GetRequiredService<IEmbeddings>(), services);
    }
}
