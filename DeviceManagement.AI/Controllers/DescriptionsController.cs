using DeviceManagement.AI.Models;
using DeviceManagement.AI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagement.AI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DescriptionsController : ControllerBase
{
    private readonly IDeviceDescriptionService _descriptionService;
    private readonly ILogger<DescriptionsController> _logger;

    public DescriptionsController(IDeviceDescriptionService descriptionService, ILogger<DescriptionsController> logger)
    {
        _descriptionService = descriptionService;
        _logger = logger;
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateDescriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GenerateDescriptionResponse>> Generate([FromBody] GenerateDescriptionRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var description = await _descriptionService.GenerateDescriptionAsync(request, cancellationToken);
            return Ok(new GenerateDescriptionResponse { Description = description });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Gemini API request failed while generating device description.");

            var message = ex.Message;
            if (message.Contains("429", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("quota", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    "Gemini quota exceeded for this API key/project. Enable billing or wait for quota reset in Google AI Studio.");
            }

            if (message.Contains("404", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("not found for API version", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    "Configured Gemini model is not available for this API key/project. Check available models in Google AI Studio.");
            }

            return StatusCode(StatusCodes.Status502BadGateway, "AI provider request failed.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "AI configuration/response issue while generating device description.");
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}
