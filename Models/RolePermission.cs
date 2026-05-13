using System.ComponentModel.DataAnnotations;

namespace xiaoliran.Models
{
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required]
        public int PermissionId { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
