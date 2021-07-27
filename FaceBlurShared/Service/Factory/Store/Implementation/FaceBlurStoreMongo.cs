using FaceBlurShared.Models;
using FaceBlurShared.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceBlurShared.Services.Factory.Store.Implentation
{
    public class FaceBlurStoreMongo : IFaceBlurStore
    {
        private IMongoCollection<FaceBlurItem> _items;

        public FaceBlurStoreMongo(string connectionString)
        {
            var connection = new MongoUrlBuilder(connectionString);
            MongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(connection.DatabaseName);
            _items = database.GetCollection<FaceBlurItem>("face_blur");
        }
        public async Task<FaceBlurItem> Create(Uri uri)
        {
            var item = new FaceBlurItem() { originUrl = uri.ToString()};
            await _items.InsertOneAsync(item);
            return item;
        }

        public async Task<FaceBlurItem> Get(string Id)
        {
            var oid = ObjectId.Parse(Id);
            return await _items.Find(new BsonDocument("_id", oid)).FirstOrDefaultAsync();
        }

        public async Task<FaceBlurItem> GetByHash(string hash)
        {
            return await _items.Find(new BsonDocument("hash", hash)).FirstOrDefaultAsync();
        }

        public async Task<FaceBlurItem> GetByUrl(Uri uri)
        {
            return await _items.Find(new BsonDocument("originUrl", uri.ToString())).FirstOrDefaultAsync();
        }

        public async Task<FaceBlurItem> Update(FaceBlurItem item)
        {
            await _items.ReplaceOneAsync(new BsonDocument("_id", new ObjectId(item.Id)), item);
            return item;
        }
    }
}
