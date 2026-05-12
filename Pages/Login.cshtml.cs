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
                    return RedirectToPage("/Index");
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
