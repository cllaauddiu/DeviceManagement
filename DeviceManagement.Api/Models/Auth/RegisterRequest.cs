using System.ComponentModel.DataAnnotations;

namespace DeviceManagement.Api.Models.Auth;

public class RegisterRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
