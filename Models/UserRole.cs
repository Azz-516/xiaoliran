using System.ComponentModel.DataAnnotations;

namespace xiaoliran.Models
{
    public class UserRole
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int RoleId { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
