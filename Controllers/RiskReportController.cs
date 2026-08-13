using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIC.risk.DTOs;
using MIC.risk.Extensions;
using MIC.risk.Mappers;
using MIC.risk.Services.Interfaces;

namespace MIC.risk.Controllers;

[Authorize]
[ApiController]
[Route("api/risk-report")]
public class RiskReportController : ControllerBase
{
    private readonly IRiskReportService _riskReportService;
    private readonly IEmployeeService _employeeService;
    private readonly IAuthorizationService _authorizationService;

    public RiskReportController(
        IRiskReportService riskReportService,
        IEmployeeService employeeService,
        IAuthorizationService authorizationService)
    {
        _riskReportService = riskReportService;
        _employeeService = employeeService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResultDto<RiskReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationHelper.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var reports = await _riskReportService.GetAllAsync(status, page, pageSize, cancellationToken);
        return Ok(reports);
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<RiskReportResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var employee = await RequireActiveEmployeeAsync(cancellationToken);
        var reports = await _riskReportService.GetByEmployeeIdAsync(employee.Id, cancellationToken);
        return Ok(reports);
    }

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

    [HttpPost]
    [ProducesResponseType(typeof(RiskReportResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateRiskReportRequestDto dto, CancellationToken cancellationToken)
    {
        var employee = await RequireActiveEmployeeAsync(cancellationToken);

        if (!User.IsAdmin() && dto.EmpId != employee.Id)
        {
            return Forbid();
        }

        await _employeeService.EnsureActiveByIdAsync(dto.EmpId, cancellationToken);

        var createdReport = await _riskReportService.CreateReportAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = createdReport.Id }, createdReport);
    }

    [HttpPost("{id:long}/auditor-evaluation")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RiskReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AttachAuditorEvaluation(
        long id,
        [FromBody] CreateEvaluationRequestDto dto,
        CancellationToken cancellationToken)
    {
        var admin = await RequireActiveEmployeeAsync(cancellationToken);
        var updatedReport = await _riskReportService.AttachAuditorEvaluationAsync(id, dto, admin.Id, cancellationToken);
        if (updatedReport == null)
        {
            return NotFound(new { Message = $"Risk report with ID {id} was not found." });
        }

        return Ok(updatedReport);
    }

    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RiskReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateRiskReportStatusRequestDto dto,
        CancellationToken cancellationToken)
    {
        var admin = await RequireActiveEmployeeAsync(cancellationToken);
        var updatedReport = await _riskReportService.UpdateStatusAsync(id, dto.NewStatus, admin.Id, cancellationToken);
        if (updatedReport == null)
        {
            return NotFound(new { Message = $"Risk report with ID {id} was not found." });
        }

        return Ok(updatedReport);
    }

    [HttpGet("{id:long}/history")]
    [ProducesResponseType(typeof(PagedResultDto<RiskReportStatusHistoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatusHistory(
        long id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationHelper.DefaultPageSize,
        CancellationToken cancellationToken = default)
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

        var history = await _riskReportService.GetStatusHistoryAsync(id, page, pageSize, cancellationToken);
        return Ok(history);
    }

    private async Task<EmployeeResponseDto> RequireActiveEmployeeAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        await _employeeService.EnsureActiveByIdentityUserIdAsync(userId, cancellationToken);
        return (await _employeeService.GetByIdentityUserIdAsync(userId, cancellationToken))!;
    }
}
