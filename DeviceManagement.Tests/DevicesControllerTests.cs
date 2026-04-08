using System.Net;
using System.Net.Http.Json;
using DeviceManagement.Api.Models;

namespace DeviceManagement.Tests;

[Collection("Integration")]
public class DevicesControllerTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public DevicesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.CleanDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static Device MakeDevice(string name = "Test Phone") => new()
    {
        Name = name,
        Manufacturer = "TestCorp",
        Type = "phone",
        OS = "Android",
        OSVersion = "14",
        Processor = "Snapdragon",
        RamAmount = 8,
        Description = "Test device"
    };

    [Fact]
    public async Task GetAll_EmptyDb_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/devices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var devices = await response.Content.ReadFromJsonAsync<List<Device>>();
        Assert.Empty(devices!);
    }

    [Fact]
    public async Task Create_ValidDevice_Returns201WithLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/devices", MakeDevice());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<Device>();
        Assert.NotNull(created!.Id);
        Assert.Equal("Test Phone", created.Name);
    }

    [Fact]
    public async Task GetById_ExistingDevice_ReturnsDevice()
    {
        var created = await CreateDeviceAsync(MakeDevice("Galaxy S23"));

        var response = await _client.GetAsync($"/api/devices/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<Device>();
        Assert.Equal("Galaxy S23", fetched!.Name);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/devices/000000000000000000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_AfterInsertingTwo_ReturnsBoth()
    {
        await CreateDeviceAsync(MakeDevice("Device A"));
        await CreateDeviceAsync(MakeDevice("Device B"));

        var devices = await _client.GetFromJsonAsync<List<Device>>("/api/devices");

        Assert.Equal(2, devices!.Count);
    }

    [Fact]
    public async Task Update_ExistingDevice_Returns204AndPersistsChanges()
    {
        var created = await CreateDeviceAsync(MakeDevice());
        created.Name = "Updated Phone";

        var putResponse = await _client.PutAsJsonAsync($"/api/devices/{created.Id}", created);
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var fetched = await _client.GetFromJsonAsync<Device>($"/api/devices/{created.Id}");
        Assert.Equal("Updated Phone", fetched!.Name);
    }

    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/devices/000000000000000000000000", MakeDevice());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingDevice_Returns204AndIsGone()
    {
        var created = await CreateDeviceAsync(MakeDevice());

        var deleteResponse = await _client.DeleteAsync($"/api/devices/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/devices/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/devices/000000000000000000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Device> CreateDeviceAsync(Device device)
    {
        var response = await _client.PostAsJsonAsync("/api/devices", device);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Device>())!;
    }
}
