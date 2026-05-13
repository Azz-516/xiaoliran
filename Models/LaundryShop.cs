using System.ComponentModel.DataAnnotations;

namespace xiaoliran.Models
{
    public class LaundryShop
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string ContactPhone { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ContactPerson { get; set; }

        [Required, MaxLength(10)]
        public string Status { get; set; } = "营业中";

        [MaxLength(50)]
        public string? BusinessHours { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
