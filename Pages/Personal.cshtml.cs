using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using xiaoliran.Data;

namespace xiaoliran.Pages
{
    public class PersonalModel : PageModel
    {
        private readonly AppDbContext _db;

        public PersonalModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public string RealName { get; set; } = string.Empty;

        [BindProperty]
        public string Gender { get; set; } = "男";

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Login");
            }

            var user = await _db.TbUsers.FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            Username = user.Username;
            RealName = user.RealName;
            Gender = user.Gender;
            Password = user.Password;

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Login");
            }

            var user = await _db.TbUsers.FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            user.RealName = RealName;
            user.Gender = Gender;

            if (!string.IsNullOrWhiteSpace(Password))
            {
                user.Password = Password;
            }

            await _db.SaveChangesAsync();

            HttpContext.Session.SetString("RealName", RealName);
            HttpContext.Session.SetString("Gender", Gender);

            var initial = RealName.Length > 0 ? RealName.Substring(0, 1) : "?";

            if (Request.Headers.ContainsKey("X-Requested-With"))
            {
                return new JsonResult(new
                {
                    success = true,
                    message = "保存成功",
                    realName = RealName,
                    initial = initial,
                    gender = Gender
                });
            }

            Message = "保存成功";
            IsSuccess = true;
            RealName = user.RealName;
            Gender = user.Gender;
            Password = user.Password;
            Username = user.Username;

            return Page();
        }
    }
}
