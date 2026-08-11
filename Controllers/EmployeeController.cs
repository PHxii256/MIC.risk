using Microsoft.AspNetCore.Mvc;
using MIC.risk.DTOs;
using MIC.risk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace MIC.risk.Controllers;

[ApiController]
[Route("api/employee")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmployeeResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var employees = await _employeeService.GetAllAsync(cancellationToken);
        return Ok(employees);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(EmployeeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(id, cancellationToken);
        if (employee == null)
        {
            return NotFound(new { Message = $"Employee with ID {id} was not found." });
        }

        return Ok(employee);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EmployeeResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var createdEmployee = await _employeeService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(
                nameof(GetById),
                new { id = createdEmployee.Id },
                createdEmployee);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(EmployeeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateEmployeeRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updatedEmployee = await _employeeService.UpdateAsync(id, dto, cancellationToken);
            if (updatedEmployee == null)
            {
                return NotFound(new { Message = $"Employee with ID {id} was not found." });
            }

            return Ok(updatedEmployee);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPatch("{id:long}/toggle-active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(long id, CancellationToken cancellationToken)
    {
        var success = await _employeeService.ToggleActiveStatusAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { Message = $"Employee with ID {id} was not found." });
        }

        return NoContent();
    }
}