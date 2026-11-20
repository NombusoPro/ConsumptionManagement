using System.ComponentModel.DataAnnotations;

namespace ConsumptionManagement.API.Dtos
{
    public class CreateReadingRequest
    {
        [Required(ErrorMessage = "TimestampUtc is required.")]
        public string? TimestampUtc { get; set; }

        [Required(ErrorMessage = "CumulativeKwh is required.")]
        public decimal? CumulativeKwh { get; set; }
    }

    public record ReadingDto(int Id, int MeterId, DateTime TimestampUtc, decimal CumulativeKwh);
}
