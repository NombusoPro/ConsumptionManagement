using ConsumptionManagement.API.Models;

namespace ConsumptionManagement.API.Services
{
    public interface IConsumptionService
    {
        ConsumptionSummaryDto CalculateConsumption(
            IEnumerable<ConsumptionReading> readings,
            DateTime fromUtc,
            DateTime toUtc);
    }
}
