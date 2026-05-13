using System.ComponentModel.DataAnnotations;

namespace xiaoliran.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string RoleKey { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
