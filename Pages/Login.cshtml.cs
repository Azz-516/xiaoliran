using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using xiaoliran.Data;

namespace xiaoliran.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _db;

        public LoginModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost()
        {
            try
            {
                var user = await _db.TbUsers.FirstOrDefaultAsync(u => u.Username == Username);

                if (user != null && user.Password == Password)
                {
                    var roleKeys = await (
                        from ur in _db.UserRoles
                        join r in _db.Roles on ur.RoleId equals r.Id
                        where ur.UserId == user.Id
                        select r.RoleKey
                    ).ToListAsync();

                    var permissionKeys = await (
                        from rp in _db.RolePermissions
                        join p in _db.Permissions on rp.PermissionId equals p.Id
                        join ur in _db.UserRoles on rp.RoleId equals ur.RoleId
                        where ur.UserId == user.Id
                        select p.PermissionKey
                    ).ToListAsync();

                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("UserName", user.Username);
                    HttpContext.Session.SetString("RealName", user.RealName);
                    HttpContext.Session.SetString("Gender", user.Gender);
                    HttpContext.Session.SetString("Phone", user.Phone ?? "");
                    HttpContext.Session.SetString("UserRoles", string.Join(",", roleKeys));
                    HttpContext.Session.SetString("UserPermissions", string.Join(",", permissionKeys));
                    return RedirectToPage("/Dashboard");
                }

                ErrorMessage = "用户名或密码错误";
            }
            catch (Exception)
            {
                ErrorMessage = "服务异常，请稍后重试";
            }

            return Page();
        }
    }
}
