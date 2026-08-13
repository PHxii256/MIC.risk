using MIC.risk.DTOs;

namespace MIC.risk.Services.Interfaces;

public interface IResourceService
{
    Task<IEnumerable<ResourceResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResourceResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ResourceResponseDto> CreateAsync(CreateResourceRequestDto dto, CancellationToken cancellationToken = default);
    Task<ResourceResponseDto?> UpdateAsync(long id, UpdateResourceRequestDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
