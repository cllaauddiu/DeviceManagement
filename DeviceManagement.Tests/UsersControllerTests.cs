using System.Net;
using System.Net.Http.Json;
using DeviceManagement.Api.Models;

namespace DeviceManagement.Tests;

[Collection("Integration")]
public class UsersControllerTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.CleanDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static User MakeUser(string name = "Test User") => new()
    {
        Name = name,
        Role = "Engineer",
        Location = "Cluj-Napoca"
    };

    [Fact]
    public async Task GetAll_EmptyDb_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<User>>();
        Assert.Empty(users!);
    }

    [Fact]
    public async Task Create_ValidUser_Returns201WithLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/users", MakeUser());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(created!.Id);
        Assert.Equal("Test User", created.Name);
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsUser()
    {
        var created = await CreateUserAsync(MakeUser("Popescu Ion"));

        var response = await _client.GetAsync($"/api/users/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<User>();
        Assert.Equal("Popescu Ion", fetched!.Name);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/users/000000000000000000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_AfterInsertingTwo_ReturnsBoth()
    {
        await CreateUserAsync(MakeUser("User A"));
        await CreateUserAsync(MakeUser("User B"));

        var users = await _client.GetFromJsonAsync<List<User>>("/api/users");

        Assert.Equal(2, users!.Count);
    }

    [Fact]
    public async Task Update_ExistingUser_Returns204AndPersistsChanges()
    {
        var created = await CreateUserAsync(MakeUser());
        created.Role = "Senior Engineer";

        var putResponse = await _client.PutAsJsonAsync($"/api/users/{created.Id}", created);
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var fetched = await _client.GetFromJsonAsync<User>($"/api/users/{created.Id}");
        Assert.Equal("Senior Engineer", fetched!.Role);
    }

    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/users/000000000000000000000000", MakeUser());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingUser_Returns204AndIsGone()
    {
        var created = await CreateUserAsync(MakeUser());

        var deleteResponse = await _client.DeleteAsync($"/api/users/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/users/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/users/000000000000000000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<User> CreateUserAsync(User user)
    {
        var response = await _client.PostAsJsonAsync("/api/users", user);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<User>())!;
    }
}
