using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.Models
{
    public class Resource
    {
        public long Id { get; set; }
        [Column("Name")]
        public string Name { get; set; } = null!;

        // FK to Employee
        public long EmpId { get; set; }
        public Employee Employee { get; set; } = null!;

        public string Url { get; set; } = null!;

        // FK to ResourceType
        public string ResourceTypeName { get; set; } = null!;
        public ResourceType ResourceType { get; set; } = null!;

        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}