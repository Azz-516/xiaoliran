using System.ComponentModel.DataAnnotations;

namespace xiaoliran.Models
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string PermissionKey { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string PermissionName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Module { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
