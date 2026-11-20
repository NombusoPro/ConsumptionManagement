using System.ComponentModel.DataAnnotations;

namespace ConsumptionManagement.API.Models
{
    public class Site
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Address { get; set; }
        public ICollection<Meter> Meters { get; set; } = new List<Meter>();
    }
}
