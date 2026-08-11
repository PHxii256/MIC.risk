using Microsoft.EntityFrameworkCore;
using MIC.risk.Data;
using MIC.risk.DTOs;
using MIC.risk.Mappers;
using MIC.risk.Models;
using MIC.risk.Services.Interfaces;

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
        var report = await _context.RiskReports
            .AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.SubCategory)
            .Include(r => r.ReportedEvaluation).ThenInclude(ev => ev.Employee)
            .Include(r => r.AuditorEvaluation).ThenInclude(ev => ev!.Employee)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return report?.ToDto();
    }

    public async Task<IEnumerable<RiskReportResponseDto>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.RiskReports
            .AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.SubCategory)
            .Include(r => r.ReportedEvaluation).ThenInclude(ev => ev.Employee)
            .Include(r => r.AuditorEvaluation).ThenInclude(ev => ev!.Employee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var reports = await query.ToListAsync(cancellationToken);
        return reports.Select(r => r.ToDto());
    }

    public async Task<RiskReportResponseDto> CreateReportAsync(CreateRiskReportRequestDto dto, CancellationToken cancellationToken = default)
    {
        // Execute atomic transaction for Evaluation + RiskReport creation
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Save Initial Evaluation entity first
            var evaluationEntity = dto.Evaluation.ToEntity(dto.EmpId);
            _context.RiskReportEvaluations.Add(evaluationEntity);
            await _context.SaveChangesAsync(cancellationToken);

            // 2. Save Risk Report linking the generated Evaluation ID
            var reportEntity = dto.ToEntity(evaluationEntity.Id);
            _context.RiskReports.Add(reportEntity);
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
        CancellationToken cancellationToken = default)
    {
        var report = await _context.RiskReports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        if (report == null) return null;

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Create Auditor Evaluation
            var evaluationEntity = dto.ToEntity(report.EmpId);
            _context.RiskReportEvaluations.Add(evaluationEntity);
            await _context.SaveChangesAsync(cancellationToken);

            // Assign FK
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
        UpdateRiskReportStatusRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var report = await _context.RiskReports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        if (report == null) return null;

        if (report.Status == dto.NewStatus)
        {
            return await GetByIdAsync(reportId, cancellationToken);
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Audit history track
            var history = new RiskReportStatusHistory
            {
                ReportId = reportId,
                ChangedBy = dto.ChangedByEmpId,
                OldStatus = report.Status,
                NewStatus = dto.NewStatus,
                ChangedAt = DateTimeOffset.UtcNow
            };

            _context.RiskReportStatusHistories.Add(history);

            // Update main record status
            report.Status = dto.NewStatus;

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

    public async Task<IEnumerable<RiskReportStatusHistoryResponseDto>> GetStatusHistoryAsync(
        long reportId,
        CancellationToken cancellationToken = default)
    {
        var histories = await _context.RiskReportStatusHistories
            .AsNoTracking()
            .Include(h => h.ChangedByEmployee).ThenInclude(e => e.Department)
            .Where(h => h.ReportId == reportId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);

        return histories.Select(h => h.ToDto());
    }
}