using MIC.risk.DTOs;

namespace MIC.risk.Services.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<EmployeeResponseDto?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default);
    Task EnsureActiveByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default);
    Task EnsureActiveByIdAsync(long empId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EmployeeResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmployeeResponseDto> CreateAsync(CreateEmployeeRequestDto dto, CancellationToken cancellationToken = default);
    Task<EmployeeResponseDto?> UpdateAsync(long id, UpdateEmployeeRequestDto dto, CancellationToken cancellationToken = default);
    Task<bool> ToggleActiveStatusAsync(long id, CancellationToken cancellationToken = default);
}