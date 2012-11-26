using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;


namespace NewsCrawler
{
    public class MongoRepository
    {
        private bool inst;
        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection collection;

        public string ConnectionString { get; private set; }
        public string DatabaseName { get; private set; }
        public string CollectionName { get; private set; }

        public MongoRepository(string connectionString, string dbName, string collectionName)
        {
            ConnectionString = connectionString;
            DatabaseName = dbName;
            CollectionName = collectionName;
            try
            {
                server = MongoServer.Create(connectionString);
                database = server.GetDatabase(dbName, SafeMode.True);
                collection = database.GetCollection(collectionName);
                server.Ping();
            }
            catch
            {
                throw new Exception("Проблема с сервером БД");
                //return null;
            }
        }

        public void SaveDocument(Dictionary<string, object> document)
        {
            collection.Insert(new BsonDocument(document));
        }

        public bool ExistDocument(string title, DateTime date)
        {
            var query = Query.And(Query.EQ("title", title), Query.EQ("date", date));
            var res = collection.FindAs(typeof(BsonDocument), query);
            return (res.Count() != 0);
        }

        public bool ExistDocument(string title)
        {
            var query = Query.EQ("title", title);
            var res = collection.FindAs(typeof(BsonDocument), query);
            return (res.Count() != 0);
        }

    }
}
