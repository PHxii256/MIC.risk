namespace MIC.risk.DTOs
{
    public record RiskSubcategoryResponseDto(
        long Id,
        string Name,
        string Category
    );

    public record CreateRiskSubcategoryRequestDto(
        string Name,
        string Category
    );
}