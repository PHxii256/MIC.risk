using MIC.risk.DTOs;
using MIC.risk.Models;

namespace MIC.risk.Services.Interfaces;

public interface IRiskReportService
{
    Task<RiskReport?> GetEntityByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<RiskReportResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PagedResultDto<RiskReportResponseDto>> GetAllAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<RiskReportResponseDto>> GetByEmployeeIdAsync(long empId, CancellationToken cancellationToken = default);
    Task<RiskReportResponseDto> CreateReportAsync(CreateRiskReportRequestDto dto, CancellationToken cancellationToken = default);
    Task<RiskReportResponseDto?> AttachAuditorEvaluationAsync(long reportId, CreateEvaluationRequestDto dto, long auditorEmpId, CancellationToken cancellationToken = default);
    Task<RiskReportResponseDto?> UpdateStatusAsync(long reportId, string newStatus, long changedByEmpId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<RiskReportStatusHistoryResponseDto>> GetStatusHistoryAsync(long reportId, int page, int pageSize, CancellationToken cancellationToken = default);
}
