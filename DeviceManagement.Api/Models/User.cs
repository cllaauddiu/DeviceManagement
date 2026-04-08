using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DeviceManagement.Api.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
}
