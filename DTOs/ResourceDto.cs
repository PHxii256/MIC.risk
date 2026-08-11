using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.DTOs
{
    public record CreateResourceRequestDto(
    string Name,
    long UploadedByEmpId,
    string Url,
    string ResourceTypeName
    );

    public record ResourceResponseDto(
        long Id,
        string Name,
        EmployeeResponseDto UploadedBy,
        string Url,
        string ResourceTypeName,
        DateTimeOffset UploadedAt
    );
    public record ResourceTypeResponseDto(
        string Name
    );
}