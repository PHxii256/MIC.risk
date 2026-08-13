using MIC.risk.DTOs;

namespace MIC.risk.Validation;

public static class RiskValidators
{
    public static readonly HashSet<string> ValidStatuses = new(StringComparer.Ordinal)
    {
        "Submitted", "InReview", "Resolved"
    };

    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new(StringComparer.Ordinal)
    {
        ["Submitted"] = new HashSet<string>(StringComparer.Ordinal) { "InReview", "Resolved" },
        ["InReview"] = new HashSet<string>(StringComparer.Ordinal) { "Submitted", "Resolved" },
        ["Resolved"] = new HashSet<string>(StringComparer.Ordinal) { "InReview" }
    };

    public static void ValidateEvaluation(CreateEvaluationRequestDto dto)
    {
        if (dto.Severity is < 1 or > 5)
        {
            throw new InvalidOperationException("Severity must be between 1 and 5.");
        }

        if (dto.Frequency is < 1 or > 5)
        {
            throw new InvalidOperationException("Frequency must be between 1 and 5.");
        }

        if (dto.MeasuresEffectiveness is < 1 or > 5)
        {
            throw new InvalidOperationException("MeasuresEffectiveness must be between 1 and 5.");
        }
    }

    public static void ValidateStatus(string status)
    {
        if (!ValidStatuses.Contains(status))
        {
            throw new InvalidOperationException($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
        }
    }

    public static void ValidateStatusTransition(string currentStatus, string newStatus)
    {
        ValidateStatus(currentStatus);
        ValidateStatus(newStatus);

        if (currentStatus == newStatus)
        {
            return;
        }

        if (!AllowedTransitions.TryGetValue(currentStatus, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new InvalidOperationException($"Cannot transition from '{currentStatus}' to '{newStatus}'.");
        }
    }
}
