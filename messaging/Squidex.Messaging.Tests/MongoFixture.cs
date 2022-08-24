// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;

namespace Squidex.Messaging
{
    public sealed class MongoFixture : IDisposable
    {
        private readonly List<Predicate<string>> collectionsToClean = new List<Predicate<string>>();

        public IMongoDatabase Database { get; }

        public MongoFixture()
        {
            var mongoClient = new MongoClient("mongodb://localhost:27017");
            var mongoDatabase = mongoClient.GetDatabase("Messaging_Tests");

            Database = mongoDatabase;

            Dispose();
        }

        public void CleanCollections(Predicate<string> predicate)
        {
            collectionsToClean.Add(predicate);
        }

        public void Dispose()
        {
            var collections = Database.ListCollectionNames().ToList();

            foreach (var collectionName in collections.Where(x => collectionsToClean.Any(p => p(x))))
            {
                // Database.DropCollection(collectionName);
            }
        }
    }
}
