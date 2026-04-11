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

    [Fact]
    public async Task Search_RanksResultsByRelevance_DeterministicOrder()
    {
        await CreateDeviceAsync(new Device
        {
            Name = "Galaxy S23",
            Manufacturer = "Samsung",
            Type = "phone",
            OS = "Android",
            OSVersion = "14",
            Processor = "Snapdragon 8 Gen 2",
            RamAmount = 8,
            Description = ""
        });

        await CreateDeviceAsync(new Device
        {
            Name = "Office Phone",
            Manufacturer = "Samsung",
            Type = "phone",
            OS = "Android",
            OSVersion = "13",
            Processor = "Snapdragon 7",
            RamAmount = 8,
            Description = ""
        });

        await CreateDeviceAsync(new Device
        {
            Name = "ThinkPad X1",
            Manufacturer = "Lenovo",
            Type = "laptop",
            OS = "Windows",
            OSVersion = "11",
            Processor = "Intel Core i7",
            RamAmount = 16,
            Description = ""
        });

        var results = await _client.GetFromJsonAsync<List<Device>>("/api/devices/search?q=galaxy samsung 8gb");

        Assert.NotNull(results);
        Assert.True(results!.Count >= 2);
        Assert.Equal("Galaxy S23", results[0].Name);
        Assert.Contains(results, d => d.Name == "Office Phone");
        Assert.DoesNotContain(results, d => d.Name == "ThinkPad X1");
    }

    [Fact]
    public async Task Search_IsCaseInsensitive_AndIgnoresPunctuationAndExtraSpaces()
    {
        await CreateDeviceAsync(new Device
        {
            Name = "Pixel 8",
            Manufacturer = "Google",
            Type = "phone",
            OS = "Android",
            OSVersion = "14",
            Processor = "Tensor G3",
            RamAmount = 8,
            Description = ""
        });

        var results = await _client.GetFromJsonAsync<List<Device>>("/api/devices/search?q=   TENSOR,,,   g3!!!   ");

        Assert.NotNull(results);
        Assert.Single(results!);
        Assert.Equal("Pixel 8", results[0].Name);
    }

    [Fact]
    public async Task Search_MatchesRamField()
    {
        await CreateDeviceAsync(new Device
        {
            Name = "Workstation A",
            Manufacturer = "Dell",
            Type = "desktop",
            OS = "Windows",
            OSVersion = "11",
            Processor = "Intel Xeon",
            RamAmount = 32,
            Description = ""
        });

        await CreateDeviceAsync(new Device
        {
            Name = "Workstation B",
            Manufacturer = "Dell",
            Type = "desktop",
            OS = "Windows",
            OSVersion = "11",
            Processor = "Intel Xeon",
            RamAmount = 16,
            Description = ""
        });

        var results = await _client.GetFromJsonAsync<List<Device>>("/api/devices/search?q=32GB");

        Assert.NotNull(results);
        Assert.Single(results!);
        Assert.Equal("Workstation A", results[0].Name);
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsEmptyList()
    {
        await CreateDeviceAsync(MakeDevice("Device A"));

        var results = await _client.GetFromJsonAsync<List<Device>>("/api/devices/search?q=   ");

        Assert.NotNull(results);
        Assert.Empty(results!);
    }

    private async Task<Device> CreateDeviceAsync(Device device)
    {
        var response = await _client.PostAsJsonAsync("/api/devices", device);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Device>())!;
    }
}
