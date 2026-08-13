using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.DTOs
{
    public record RecordResourceEngagementRequestDto(
        long EmpId,
        long ResourceId,
        bool Viewed,
        bool? SurveyCompleted
    );

    public record ResourceEngagementResponseDto(
        long Id,
        EmployeeResponseDto Employee,
        ResourceResponseDto Resource,
        bool Viewed,
        bool? SurveyCompleted,
        DateTimeOffset? ViewedAt,
        DateTimeOffset? CompletedAt
    );

    public record ResourceEngagementStatsDto(
        long ResourceId,
        string ResourceName,
        string ResourceType,
        int ViewCount,
        int QuizCompletionCount,
        double CompletionRate
    );

    public record DepartmentEngagementStatsDto(
        long DepartmentId,
        string DepartmentName,
        int ActiveEmployees,
        int EmployeesWithQuizCompletion,
        double AwarenessPercentage
    );
}