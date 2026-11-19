using System.ComponentModel.DataAnnotations;

namespace ConsumptionManagement.API.Models
{
    public class Meter
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SiteId { get; set; }

        [Required]
        [MaxLength(100)]
        public string MeterNumber { get; set; } = string.Empty;

        // Navigation properties
        public Site Site { get; set; } = null!;
        public ICollection<MeterReading> Readings { get; set; } = new List<MeterReading>();
    }
}
