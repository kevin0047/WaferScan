using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace WaferScan.Models
{
    public class ImageInfo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string OriginalName { get; set; }
        public string SavedName { get; set; }
        public DateTime GeneratedTime { get; set; }
    }
}