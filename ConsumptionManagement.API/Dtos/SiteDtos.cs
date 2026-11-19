using System.ComponentModel.DataAnnotations;

namespace ConsumptionManagement.API.Dtos
{
    public class CreateSiteRequest : IValidatableObject
    {
        [MaxLength(150)]
        public string? Name { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult("Name is required.", [nameof(Name)]);
            }
        }
    }

    public record SiteDto(int Id, string Name, string? Address);
}
