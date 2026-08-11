using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MIC.risk.DTOs;
using MIC.risk.Models;

namespace MIC.risk.Mappers
{
    public static class ResourceMappers
    {
        public static ResourceResponseDto ToDto(this Resource resource)
        {
            return new ResourceResponseDto(
                resource.Id,
                resource.Name,
                resource.Employee != null ? resource.Employee.ToDto() : null!,
                resource.Url,
                resource.ResourceTypeName,
                resource.UploadedAt
            );
        }

        public static Resource ToEntity(this CreateResourceRequestDto dto)
        {
            return new Resource
            {
                Name = dto.Name,
                EmpId = dto.UploadedByEmpId,
                Url = dto.Url,
                ResourceTypeName = dto.ResourceTypeName
            };
        }

        public static ResourceEngagementResponseDto ToDto(this ResourceEngagement engagement)
        {
            return new ResourceEngagementResponseDto(
                engagement.Id,
                engagement.Employee != null ? engagement.Employee.ToDto() : null!,
                engagement.Resource != null ? engagement.Resource.ToDto() : null!,
                engagement.Viewed,
                engagement.SurveyCompleted
            );
        }
    }
}
