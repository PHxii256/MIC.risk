using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.Models
{
    public class RiskReportEvaluation
    {
        public long Id { get; set; }

        // FK to Employee
        public long EmpId { get; set; }
        public Employee Employee { get; set; } = null!;

        public int Severity { get; set; }
        public int Frequency { get; set; }
        public int MeasuresEffectiveness { get; set; }

        // Computed in database: Severity * Frequency
        public int RiskScore { get; private set; }

        public string? ExistingMeasures { get; set; }
        public string? ProposedMeasures { get; set; }
        public int Priority { get; set; } = 1;
        public DateTimeOffset EvaluatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}