// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using MongoDB.Bson;
using MongoDB.Driver;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events.Mongo;

public record struct MongoVersionInfo(MongoDerivate Dervivate, int Major)
{
    public static async Task<MongoVersionInfo> DetectAsync(IMongoDatabase database,
        CancellationToken ct = default)
    {
        var command =
            new BsonDocumentCommand<BsonDocument>(new BsonDocument
            {
                { "buildInfo", 1 },
            });

        var document = await database.RunCommandAsync(command, cancellationToken: ct);

        var versionString = document["version"].AsString;
        var versionMajor = versionString.Split('.')[0];

        int.TryParse(versionMajor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int version);

        var serverString = document.ToString().ToLowerInvariant();
        bool Contains(string expected)
        {
            return serverString.Contains(expected, StringComparison.OrdinalIgnoreCase);
        }

        var derivate = MongoDerivate.MongoDB;
        if (Contains("ferret"))
        {
            derivate = MongoDerivate.FerretDB;
        }
        else if (Contains("cosmos"))
        {
            derivate = MongoDerivate.CosmosDB;
        }
        else if (Contains("amazon") || Contains("docdb") || Contains("documentdb"))
        {
            derivate = MongoDerivate.DocumentDB;
        }

        return new MongoVersionInfo(derivate, version);
    }
}

public enum MongoDerivate
{
    MongoDB,
    FerretDB,
    DocumentDB,
    CosmosDB,
}
