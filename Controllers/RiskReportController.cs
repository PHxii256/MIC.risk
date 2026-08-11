using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIC.risk.DTOs;
using MIC.risk.Mappers;
using MIC.risk.Services.Interfaces;

namespace MIC.risk.Controllers;

[Authorize]
[ApiController]
[Route("api/risk-report")]
public class RiskReportController : ControllerBase
{
    private readonly IRiskReportService _riskReportService;
    private readonly IAuthorizationService _authorizationService;

    public RiskReportController(
        IRiskReportService riskReportService,
        IAuthorizationService authorizationService)
    {
        _riskReportService = riskReportService;
        _authorizationService = authorizationService;
    }

    // GET: api/riskreport
    // GET: api/riskreport?status=Submitted
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<RiskReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var reports = await _riskReportService.GetAllAsync(status, cancellationToken);
        return Ok(reports);
    }

    // GET: api/risk-report/5
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(RiskReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var reportEntity = await _riskReportService.GetEntityByIdAsync(id, cancellationToken);
        if (reportEntity == null)
        {
            return NotFound(new { Message = $"Risk report with ID {id} was not found." });
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            User, reportEntity, "EditOrViewRiskReport");
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        return Ok(reportEntity.ToDto());
    }

    // POST: api/risk-report
    [HttpPost]
    [ProducesResponseType(typeof(RiskReportResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRiskReportRequestDto dto, CancellationToken cancellationToken)
    {
        var createdReport = await _riskReportService.CreateReportAsync(dto, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = createdReport.Id },
            createdReport);
    }

    // POST: api/risk-report/5/auditor-evaluation
    [HttpPost("{id:long}/auditor-evaluation")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RiskReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AttachAuditorEvaluation(
        long id,
        [FromBody] CreateEvaluationRequestDto dto,
        CancellationToken cancellationToken)
    {
        var updatedReport = await _riskReportService.AttachAuditorEvaluationAsync(id, dto, cancellationToken);
        if (updatedReport == null)
        {
            return NotFound(new { Message = $"Risk report with ID {id} was not found." });
        }

        return Ok(updatedReport);
    }

    // PATCH: api/risk-report/5/status
    [HttpPatch("{id:long}/status")]
    [ProducesResponseType(typeof(RiskReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateRiskReportStatusRequestDto dto,
        CancellationToken cancellationToken)
    {
        var updatedReport = await _riskReportService.UpdateStatusAsync(id, dto, cancellationToken);
        if (updatedReport == null)
        {
            return NotFound(new { Message = $"Risk report with ID {id} was not found." });
        }

        return Ok(updatedReport);
    }

    // GET: api/risk-report/5/history
    [HttpGet("{id:long}/history")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<RiskReportStatusHistoryResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusHistory(long id, CancellationToken cancellationToken)
    {
        var history = await _riskReportService.GetStatusHistoryAsync(id, cancellationToken);
        return Ok(history);
    }
}