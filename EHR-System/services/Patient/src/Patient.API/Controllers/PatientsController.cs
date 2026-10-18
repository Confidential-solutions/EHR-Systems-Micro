using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EHRPlatform.Services.Patient.Application.PatientManagement.Responses;
using EHRPlatform.Services.Patient.Application.Features.Patients.Commands;
using EHRPlatform.Services.Patient.Application.Features.Patients.Queries;

namespace EHRPlatform.Services.Patient.API.Controllers;

/// <summary>
/// Patient endpoints - CQRS with Redis caching and Elasticsearch search.
/// Full audit trail and domain events to Kafka.
/// </summary>
[ApiController]
[Route("api/v1/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IMediator mediator,
        ILogger<PatientsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create new patient.
    /// Generates MRN, publishes PatientCreatedEvent.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePatient(
        [FromBody] CreatePatientCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPatient), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get patient by MRN (Medical Record Number) - SLUG-BASED route.
    /// Example: GET /api/v1/patients/mrn/MRN-2024-001234
    /// </summary>
    [HttpGet("mrn/{mrnValue}")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientByMRN(
        string mrnValue,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPatientByMRNQuery { MRN = mrnValue },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get patient detail by MRN including allergies and conditions.
    /// Example: GET /api/v1/patients/mrn/MRN-2024-001234/detail
    /// </summary>
    [HttpGet("mrn/{mrnValue}/detail")]
    [ProducesResponseType(typeof(PatientDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientDetailByMRN(
        string mrnValue,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPatientDetailByMRNQuery { MRN = mrnValue },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get patient by ID (cached).
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatient(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPatientQuery { PatientId = id },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get patient with full details (cached).
    /// </summary>
    [HttpGet("{id}/detail")]
    [ProducesResponseType(typeof(PatientDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPatientDetailQuery { PatientId = id },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update patient info.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePatient(
        Guid id,
        [FromBody] UpdatePatientCommand command,
        CancellationToken cancellationToken)
    {
        command = command with { PatientId = id };
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Search patients (full-text via Elasticsearch).
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchResultDto<PatientResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchPatients(
        [FromQuery] string q = "",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new SearchPatientsQuery
            {
                SearchTerm = q,
                PageNumber = page,
                PageSize = pageSize
            },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// List all patients (paginated, cached).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResultDto<PatientResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPatients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ListPatientsQuery { PageNumber = page, PageSize = pageSize },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Add allergy to patient.
    /// </summary>
    [HttpPost("{id}/allergies")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAllergy(
        Guid id,
        [FromBody] AddAllergyCommand command,
        CancellationToken cancellationToken)
    {
        command = command with { PatientId = id };
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Add condition to patient.
    /// </summary>
    [HttpPost("{id}/conditions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddCondition(
        Guid id,
        [FromBody] AddConditionCommand command,
        CancellationToken cancellationToken)
    {
        command = command with { PatientId = id };
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Health check (anonymous).
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "patient-service", timestamp = DateTime.UtcNow });
    }
}
