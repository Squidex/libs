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
    public static async Task<MongoVersionInfo> DetectAsync(IMongoDatabase database, MongoDerivate derivate,
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
