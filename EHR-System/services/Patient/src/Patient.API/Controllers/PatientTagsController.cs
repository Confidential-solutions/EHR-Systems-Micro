using MediatR;
using Microsoft.AspNetCore.Mvc;
using EHRPlatform.BuildingBlocks.SharedKernel.Entities;
using EHRPlatform.BuildingBlocks.Common.Data.Models;
using EHRPlatform.BuildingBlocks.Contracts.DTOs;
using EHRPlatform.Services.Patient.Domain.Entities;

namespace EHRPlatform.Services.Patient.API.Controllers;

/// <summary>
/// Tag management for patients.
/// Route: GET/POST/PUT/DELETE /api/v1/patients/{patientId}/tags
/// </summary>
[ApiController]
[Route("api/v1/patients/{patientId}/tags")]
public class PatientTagsController : ControllerBase
{
    private readonly ITagQueryService _tagQueryService;
    private readonly IMediator _mediator;
    private readonly ILogger<PatientTagsController> _logger;

    public PatientTagsController(
        ITagQueryService tagQueryService,
        IMediator mediator,
        ILogger<PatientTagsController> logger)
    {
        _tagQueryService = tagQueryService;
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientTags(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = await _tagQueryService.GetResourceTagsAsync(
                patientId,
                nameof(PatientEntity),
                cancellationToken);
            return Ok(new { patientId, tags });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tags for patient {PatientId}", patientId);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyPatientTags(
        Guid patientId,
        [FromBody] ApplyTagsCommand baseCommand,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = baseCommand with
            {
                ResourceType = nameof(PatientEntity),
                ResourceId = patientId,
                ServiceName = "Patient"
            };
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying tags to patient {PatientId}", patientId);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{tagId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePatientTag(
        Guid patientId,
        Guid tagId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new RemoveTagCommand
            {
                ResourceType = nameof(PatientEntity),
                ResourceId = patientId,
                TagId = tagId,
                ServiceName = "Patient"
            };
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag from patient");
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetPatientTags(
        Guid patientId,
        [FromBody] SetResourceTagsCommand baseCommand,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = baseCommand with
            {
                ResourceType = nameof(PatientEntity),
                ResourceId = patientId,
                ServiceName = "Patient"
            };
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting tags for patient");
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}
