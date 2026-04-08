using System.ComponentModel.DataAnnotations;

namespace DeviceManagement.AI.Models;

public class GenerateDescriptionRequest
{
    [Required]
    public string Brand { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    public string? Cpu { get; set; }
    public int? RamGb { get; set; }
    public int? StorageGb { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Notes { get; set; }
}
