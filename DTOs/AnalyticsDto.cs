namespace MIC.risk.DTOs;

public record CountByLabelDto(
    string Label,
    int Count
);

public record EarlyWarningIndicatorsDto(
    int CriticalResidualRisks,
    int WeakControls,
    int PendingReview
);

public record DepartmentMaturityDto(
    string DepartmentName,
    double MaturityScore
);

public record ResidualRiskMatrixCellDto(
    int Severity,
    int Frequency,
    int Count
);

public record ResidualRiskMatrixDto(
    IEnumerable<ResidualRiskMatrixCellDto> Cells
);

public record AnalyticsDashboardDto(
    double RiskAwarenessPercentage,
    EarlyWarningIndicatorsDto EarlyWarningIndicators,
    RiskActionSummaryDto OutstandingActions,
    double? AverageRiskResolutionTimeHours,
    int RisksSubmittedThisWeek,
    int RisksSubmittedThisMonth,
    IEnumerable<CountByLabelDto> RisksByDepartment,
    IEnumerable<CountByLabelDto> RisksByLocation,
    IEnumerable<CountByLabelDto> RiskSubcategoryDistribution,
    IEnumerable<DepartmentMaturityDto> RiskMaturityByDepartment,
    ResidualRiskMatrixDto ResidualRiskMatrix
);

public record EmployeeDepartmentStatsDto(
    long DepartmentId,
    string DepartmentName,
    int ActiveEmployees,
    int EmployeesWithQuizCompletion,
    double AwarenessPercentage,
    int RiskReportCount
);
