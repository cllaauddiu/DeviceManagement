using DeviceManagement.Api.Models;
using MongoDB.Driver;

namespace DeviceManagement.Api.Services;

public class DeviceService
{
    private readonly IMongoCollection<Device> _devices;

    public DeviceService(MongoDbService mongoDbService)
    {
        _devices = mongoDbService.Database!.GetCollection<Device>("Devices");
    }

    public async Task<List<Device>> GetAllAsync() =>
        await _devices.Find(_ => true).ToListAsync();

    public async Task<Device?> GetByIdAsync(string id) =>
        await _devices.Find(d => d.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Device device) =>
        await _devices.InsertOneAsync(device);

    public async Task UpdateAsync(string id, Device device) =>
        await _devices.ReplaceOneAsync(d => d.Id == id, device);

    public async Task DeleteAsync(string id) =>
        await _devices.DeleteOneAsync(d => d.Id == id);
}
