using Microsoft.EntityFrameworkCore;
using MIC.risk.Data;
using MIC.risk.DTOs;
using MIC.risk.Mappers;
using MIC.risk.Services.Interfaces;

namespace MIC.risk.Services;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDBContext _context;

    public DepartmentService(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DepartmentResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return departments.Select(d => d.ToDto());
    }

    public async Task<DepartmentResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return department?.ToDto();
    }

    public async Task<DepartmentResponseDto> CreateAsync(CreateDepartmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("Department name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.BranchLocation))
        {
            throw new InvalidOperationException("Branch location is required.");
        }

        var entity = dto.ToEntity();
        _context.Departments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.ToDto();
    }

    public async Task<DepartmentResponseDto?> UpdateAsync(long id, CreateDepartmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (department == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("Department name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.BranchLocation))
        {
            throw new InvalidOperationException("Branch location is required.");
        }

        department.Name = dto.Name;
        department.BranchLocation = dto.BranchLocation;
        await _context.SaveChangesAsync(cancellationToken);

        return department.ToDto();
    }
}
