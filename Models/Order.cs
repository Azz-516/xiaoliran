using System.ComponentModel.DataAnnotations;

namespace xiaoliran.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string OrderNo { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }

        [Required]
        public int LaundryShopId { get; set; }

        [Required, MaxLength(20)]
        public string ServiceType { get; set; } = "洗衣";

        [MaxLength(50)]
        public string? ClothingType { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "待取件";

        public decimal EstimatedCost { get; set; }

        [MaxLength(500)]
        public string? Remark { get; set; }

        public DateTime? PickupTime { get; set; }

        public DateTime? DeliveryTime { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
