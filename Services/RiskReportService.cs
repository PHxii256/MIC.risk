using Microsoft.EntityFrameworkCore;
using MIC.risk.Data;
using MIC.risk.DTOs;
using MIC.risk.Mappers;
using MIC.risk.Models;
using MIC.risk.Services.Interfaces;
using MIC.risk.Validation;

namespace MIC.risk.Services;

public class RiskReportService : IRiskReportService
{
    private readonly ApplicationDBContext _context;

    public RiskReportService(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<RiskReport?> GetEntityByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.RiskReports
            .AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.SubCategory)
            .Include(r => r.ReportedEvaluation).ThenInclude(ev => ev.Employee)
            .Include(r => r.AuditorEvaluation).ThenInclude(ev => ev!.Employee)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<RiskReportResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var report = await GetEntityByIdAsync(id, cancellationToken);
        return report?.ToDto();
    }

    public async Task<PagedResultDto<RiskReportResponseDto>> GetAllAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);

        var query = _context.RiskReports
            .AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.SubCategory)
            .Include(r => r.ReportedEvaluation).ThenInclude(ev => ev.Employee)
            .Include(r => r.AuditorEvaluation).ThenInclude(ev => ev!.Employee)
            .OrderByDescending(r => r.SubmittedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            RiskValidators.ValidateStatus(status);
            query = query.Where(r => r.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var reports = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<RiskReportResponseDto>(
            reports.Select(r => r.ToDto()),
            normalizedPage,
            normalizedPageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)normalizedPageSize));
    }

    public async Task<IEnumerable<RiskReportResponseDto>> GetByEmployeeIdAsync(long empId, CancellationToken cancellationToken = default)
    {
        var reports = await _context.RiskReports
            .AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.SubCategory)
            .Include(r => r.ReportedEvaluation).ThenInclude(ev => ev.Employee)
            .Include(r => r.AuditorEvaluation).ThenInclude(ev => ev!.Employee)
            .Where(r => r.EmpId == empId)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);

        return reports.Select(r => r.ToDto());
    }

    public async Task<RiskReportResponseDto> CreateReportAsync(CreateRiskReportRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            throw new InvalidOperationException("Report description is required.");
        }

        RiskValidators.ValidateEvaluation(dto.Evaluation);

        var subCategory = await _context.RiskSubCategories
            .FirstOrDefaultAsync(sc => sc.Id == dto.SubCategoryId && sc.Active, cancellationToken);
        if (subCategory == null)
        {
            throw new InvalidOperationException("Subcategory does not exist or is inactive.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var evaluationEntity = dto.Evaluation.ToEntity(dto.EmpId);
            _context.RiskReportEvaluations.Add(evaluationEntity);
            await _context.SaveChangesAsync(cancellationToken);

            var reportEntity = dto.ToEntity(evaluationEntity.Id);
            _context.RiskReports.Add(reportEntity);
            await _context.SaveChangesAsync(cancellationToken);

            var initialHistory = new RiskReportStatusHistory
            {
                ReportId = reportEntity.Id,
                ChangedBy = dto.EmpId,
                OldStatus = "Submitted",
                NewStatus = "Submitted",
                ChangedAt = reportEntity.SubmittedAt
            };
            _context.RiskReportStatusHistories.Add(initialHistory);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return (await GetByIdAsync(reportEntity.Id, cancellationToken))!;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RiskReportResponseDto?> AttachAuditorEvaluationAsync(
        long reportId,
        CreateEvaluationRequestDto dto,
        long auditorEmpId,
        CancellationToken cancellationToken = default)
    {
        RiskValidators.ValidateEvaluation(dto);

        var report = await _context.RiskReports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        if (report == null) return null;

        if (report.AuditorEvaluationId.HasValue)
        {
            throw new InvalidOperationException("An auditor evaluation already exists for this report.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var evaluationEntity = dto.ToEntity(auditorEmpId);
            _context.RiskReportEvaluations.Add(evaluationEntity);
            await _context.SaveChangesAsync(cancellationToken);

            report.AuditorEvaluationId = evaluationEntity.Id;
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(reportId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RiskReportResponseDto?> UpdateStatusAsync(
        long reportId,
        string newStatus,
        long changedByEmpId,
        CancellationToken cancellationToken = default)
    {
        var report = await _context.RiskReports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        if (report == null) return null;

        RiskValidators.ValidateStatusTransition(report.Status, newStatus);

        if (report.Status == newStatus)
        {
            return await GetByIdAsync(reportId, cancellationToken);
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var history = new RiskReportStatusHistory
            {
                ReportId = reportId,
                ChangedBy = changedByEmpId,
                OldStatus = report.Status,
                NewStatus = newStatus,
                ChangedAt = DateTimeOffset.UtcNow
            };

            _context.RiskReportStatusHistories.Add(history);

            report.Status = newStatus;
            report.ResolvedAt = newStatus == "Resolved" ? DateTimeOffset.UtcNow : null;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(reportId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResultDto<RiskReportStatusHistoryResponseDto>> GetStatusHistoryAsync(
        long reportId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);

        var query = _context.RiskReportStatusHistories
            .AsNoTracking()
            .Include(h => h.ChangedByEmployee).ThenInclude(e => e.Department)
            .Where(h => h.ReportId == reportId)
            .OrderByDescending(h => h.ChangedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var histories = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<RiskReportStatusHistoryResponseDto>(
            histories.Select(h => h.ToDto()),
            normalizedPage,
            normalizedPageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)normalizedPageSize));
    }
}
