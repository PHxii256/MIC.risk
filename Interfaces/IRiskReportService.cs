using MIC.risk.DTOs;
using MIC.risk.Models;

namespace MIC.risk.Services.Interfaces;

public interface IRiskReportService
{
    Task<RiskReport?> GetEntityByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<RiskReportResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<RiskReportResponseDto>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<RiskReportResponseDto> CreateReportAsync(CreateRiskReportRequestDto dto, CancellationToken cancellationToken = default);
    Task<RiskReportResponseDto?> AttachAuditorEvaluationAsync(long reportId, CreateEvaluationRequestDto dto, CancellationToken cancellationToken = default);
    Task<RiskReportResponseDto?> UpdateStatusAsync(long reportId, UpdateRiskReportStatusRequestDto dto, CancellationToken cancellationToken = default);
    Task<IEnumerable<RiskReportStatusHistoryResponseDto>> GetStatusHistoryAsync(long reportId, CancellationToken cancellationToken = default);
}