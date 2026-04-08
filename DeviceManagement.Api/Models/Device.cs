using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DeviceManagement.Api.Models;

public class Device
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string OS { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;
    public string Processor { get; set; } = string.Empty;
    public int RamAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? UserId { get; set; }
}
