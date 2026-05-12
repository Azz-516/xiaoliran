using System.ComponentModel.DataAnnotations;

namespace xiaoliran.Models
{
    public class TbUser
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string RealName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Gender { get; set; } = "男";

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
