using DeviceManagement.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace DeviceManagement.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestDatabaseName = "DeviceManagementDB_Test";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("MongoDbSettings:DatabaseName", TestDatabaseName);
    }

    public IMongoDatabase GetTestDatabase()
    {
        var mongoDbService = Services.GetRequiredService<MongoDbService>();
        return mongoDbService.Database!;
    }

    public async Task CleanDatabaseAsync()
    {
        var db = GetTestDatabase();
        await db.DropCollectionAsync("Devices");
        await db.DropCollectionAsync("Users");
    }
}
