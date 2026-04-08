namespace DeviceManagement.Api.Models.Auth;

public record LoginResponse(string Id, string Email, string Name, string Token);
