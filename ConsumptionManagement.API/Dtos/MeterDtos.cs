using System.ComponentModel.DataAnnotations;

namespace ConsumptionManagement.API.Dtos
{
    public class CreateMeterRequest : IValidatableObject
    {
        [MaxLength(100)]
        public string? MeterNumber { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(MeterNumber))
            {
                yield return new ValidationResult("MeterNumber is required.", [nameof(MeterNumber)]);
            }
        }
    }

    public record MeterDto(int Id, int SiteId, string MeterNumber);
}
