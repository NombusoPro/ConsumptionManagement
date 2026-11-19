using ConsumptionManagement.API.Models;

namespace ConsumptionManagement.API.Services
{
    public static class CalculateConsumption
    {
        public static ConsumptionSummaryDto Compute(
            IEnumerable<ConsumptionReading> readings,
            DateTime fromUtc,
            DateTime toUtc)
        {
            ArgumentNullException.ThrowIfNull(readings);

            var inRange = readings
                .Where(reading => reading.TimestampUtc >= fromUtc && reading.TimestampUtc <= toUtc)
                .OrderBy(reading => reading.TimestampUtc)
                .ToList();

            var totalConsumptionKwh = inRange.Count < 2
                ? 0m
                : inRange[^1].CumulativeKwh - inRange[0].CumulativeKwh;

            var consumptionByDate = new Dictionary<DateOnly, decimal>();

            for (var index = 1; index < inRange.Count; index++)
            {
                var earlier = inRange[index - 1];
                var later = inRange[index];
                var difference = later.CumulativeKwh - earlier.CumulativeKwh;
                if (difference <= 0)
                {
                    continue;
                }

                var date = DateOnly.FromDateTime(later.TimestampUtc);
                consumptionByDate[date] = consumptionByDate.GetValueOrDefault(date) + difference;
            }

            var dailyConsumption = consumptionByDate
                .OrderBy(entry => entry.Key)
                .Select(entry => new DailyConsumptionDto(entry.Key, entry.Value))
                .ToList();

            return new ConsumptionSummaryDto(fromUtc, toUtc, totalConsumptionKwh, dailyConsumption);
        }
    }
}
