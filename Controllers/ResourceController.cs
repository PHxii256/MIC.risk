using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIC.risk.DTOs;
using MIC.risk.Services.Interfaces;

namespace MIC.risk.Controllers;

[Authorize]
[ApiController]
[Route("api/resource")]
public class ResourceController : ControllerBase
{
    private readonly IResourceService _resourceService;

    public ResourceController(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ResourceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var resources = await _resourceService.GetAllAsync(cancellationToken);
        return Ok(resources);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ResourceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetByIdAsync(id, cancellationToken);
        if (resource == null)
        {
            return NotFound(new { Message = $"Resource with ID {id} was not found." });
        }

        return Ok(resource);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ResourceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateResourceRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _resourceService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ResourceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateResourceRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _resourceService.UpdateAsync(id, dto, cancellationToken);
            if (updated == null)
            {
                return NotFound(new { Message = $"Resource with ID {id} was not found." });
            }

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var success = await _resourceService.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { Message = $"Resource with ID {id} was not found." });
        }

        return NoContent();
    }
}
