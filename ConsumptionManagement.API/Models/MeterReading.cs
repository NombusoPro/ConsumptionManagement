using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsumptionManagement.API.Models
{
    public class MeterReading
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MeterId { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ReadingSum { get; set; }

        [Required]
        public DateTime TReadingimeUtc { get; set; }
        public Meter Meter { get; set; } = null!;
    }
}
