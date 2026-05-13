using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using xiaoliran.Data;
using xiaoliran.Models;

namespace xiaoliran.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _db;

        public RegisterModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string RealName { get; set; } = string.Empty;

        [BindProperty]
        public string Gender { get; set; } = "男";

        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                Message = "请检查输入是否完整";
                IsSuccess = false;
                return Page();
            }

            if (await _db.TbUsers.AnyAsync(u => u.Username == Username))
            {
                Message = "用户名已存在";
                IsSuccess = false;
                return Page();
            }

            var user = new TbUser
            {
                Username = Username,
                Password = Password,
                RealName = RealName,
                Gender = Gender
            };

            _db.TbUsers.Add(user);
            await _db.SaveChangesAsync();

            // Assign default 'user' role
            var userRoleId = await _db.Roles
                .Where(r => r.RoleKey == "user")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (userRoleId > 0)
            {
                _db.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = userRoleId
                });
                await _db.SaveChangesAsync();
            }

            Message = "注册成功，即将跳转登录页";
            IsSuccess = true;
            return RedirectToPage("/Login");
        }
    }
}
