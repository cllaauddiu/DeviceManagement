using MongoDB.Driver;

namespace DeviceManagement.Api.Services;

public class MongoDbService
{
    public IMongoDatabase? Database { get; }

    public MongoDbService(IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("MongoDbSettings:ConnectionString").Value;
        var databaseName = configuration.GetSection("MongoDbSettings:DatabaseName").Value;

        var mongoClient = new MongoClient(connectionString);

        Database = mongoClient.GetDatabase(databaseName);
    }
}