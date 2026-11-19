using ConsumptionManagement.API.Models;
using ConsumptionCalculator = ConsumptionManagement.API.Services.CalculateConsumption;

namespace ConsumptionManagement.API.Services
{
    public class ConsumptionService : IConsumptionService
    {
        public ConsumptionSummaryDto CalculateConsumption(
            IEnumerable<ConsumptionReading> readings,
            DateTime fromUtc,
            DateTime toUtc)
        {
            return ConsumptionCalculator.Compute(readings, fromUtc, toUtc);
        }
    }
}
