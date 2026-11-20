namespace ConsumptionManagement.API.Models
{
    public record ConsumptionReading(DateTime TimestampUtc, decimal CumulativeKwh);

    public record DailyConsumptionDto(DateOnly Date, decimal ConsumptionKwh);

    public record ConsumptionSummaryDto(
        DateTime FromUtc,
        DateTime ToUtc,
        decimal TotalConsumptionKwh,
        IReadOnlyList<DailyConsumptionDto> DailyConsumption);
}
