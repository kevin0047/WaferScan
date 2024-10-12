using MongoDB.Driver;
using System;
using WaferScan.Models;

namespace WaferScan.Services
{
    public class DatabaseService
    {
        private readonly IMongoCollection<ImageInfo> _images;

        public DatabaseService(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _images = database.GetCollection<ImageInfo>("Images");
        }

        public void SaveImageInfo(ImageInfo imageInfo)
        {
            _images.InsertOne(imageInfo);
            Console.WriteLine($"이미지 정보가 데이터베이스에 저장됨: {imageInfo.OriginalName}");
        }
    }
}