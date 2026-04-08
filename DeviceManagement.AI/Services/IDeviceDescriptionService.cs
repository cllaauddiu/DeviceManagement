using DeviceManagement.AI.Models;

namespace DeviceManagement.AI.Services;

public interface IDeviceDescriptionService
{
    Task<string> GenerateDescriptionAsync(GenerateDescriptionRequest request, CancellationToken cancellationToken = default);
}
