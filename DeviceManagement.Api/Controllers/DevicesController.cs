using System.Security.Claims;
using DeviceManagement.Api.Models;
using DeviceManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly DeviceService _deviceService;

    public DevicesController(DeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Device>>> GetAll() =>
        Ok(await _deviceService.GetAllAsync());

    [HttpGet("search")]
    public async Task<ActionResult<List<Device>>> Search([FromQuery] string q) =>
        Ok(await _deviceService.SearchAsync(q));

    [HttpGet("{id}")]
    public async Task<ActionResult<Device>> GetById(string id)
    {
        var device = await _deviceService.GetByIdAsync(id);
        if (device is null) return NotFound();
        return Ok(device);
    }

    [HttpPost]
    public async Task<ActionResult> Create(Device device)
    {
        await _deviceService.CreateAsync(device);
        return CreatedAtAction(nameof(GetById), new { id = device.Id }, device);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, Device device)
    {
        var existing = await _deviceService.GetByIdAsync(id);
        if (existing is null) return NotFound();

        device.Id = id;
        await _deviceService.UpdateAsync(id, device);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var existing = await _deviceService.GetByIdAsync(id);
        if (existing is null) return NotFound();

        await _deviceService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("{id}/assign")]
    public async Task<ActionResult> Assign(string id)
    {
        var device = await _deviceService.GetByIdAsync(id);
        if (device is null) return NotFound();
        if (!string.IsNullOrEmpty(device.UserId))
            return Conflict("Dispozitivul este deja asignat unui utilizator.");

        device.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _deviceService.UpdateAsync(id, device);
        return NoContent();
    }

    [HttpPut("{id}/unassign")]
    public async Task<ActionResult> Unassign(string id)
    {
        var device = await _deviceService.GetByIdAsync(id);
        if (device is null) return NotFound();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (device.UserId != currentUserId)
            return Forbid();

        device.UserId = null;
        await _deviceService.UpdateAsync(id, device);
        return NoContent();
    }
}
