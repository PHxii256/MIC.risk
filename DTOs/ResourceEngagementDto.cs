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
        bool? SurveyCompleted
    );

}