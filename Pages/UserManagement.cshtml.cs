using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using xiaoliran.Data;
using xiaoliran.Models;

namespace xiaoliran.Pages
{
    [IgnoreAntiforgeryToken]
    public class UserManagementModel : PageModel
    {
        private readonly AppDbContext _db;

        public UserManagementModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public string SearchKey { get; set; } = string.Empty;

        public List<UserViewModel> Users { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        private const int PageSize = 15;

        public void OnGet(string searchKey = "", int p = 1)
        {
            SearchKey = searchKey;
            var query = _db.TbUsers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(u => u.Username.Contains(searchKey) || u.RealName.Contains(searchKey));
            }
            var totalCount = query.Count();
            CurrentPage = Math.Max(1, p);
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            Users = query.OrderByDescending(u => u.CreateTime)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    RealName = u.RealName,
                    Gender = u.Gender,
                    CreateTime = u.CreateTime.ToString("yyyy-MM-dd HH:mm")
                }).ToList();
        }

        public async Task<IActionResult> OnPostAdd()
        {
            try
            {
                var username = Request.Form["Username"].ToString();
                var password = Request.Form["Password"].ToString();
                var realName = Request.Form["RealName"].ToString();
                var gender = Request.Form["Gender"].ToString();

                if (await _db.TbUsers.AnyAsync(u => u.Username == username))
                {
                    return new JsonResult(new { success = false, message = "用户名已存在" });
                }

                var user = new TbUser { Username = username, Password = password, RealName = realName, Gender = gender };
                _db.TbUsers.Add(user);
                await _db.SaveChangesAsync();

                var userRoleId = await _db.Roles.Where(r => r.RoleKey == "user").Select(r => r.Id).FirstOrDefaultAsync();
                if (userRoleId > 0)
                {
                    _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRoleId });
                    await _db.SaveChangesAsync();
                }

                return new JsonResult(new { success = true, message = "添加成功" });
            }
            catch (Exception)
            {
                return new JsonResult(new { success = false, message = "服务异常，请稍后重试" });
            }
        }

        public async Task<IActionResult> OnPostEdit()
        {
            try
            {
                var id = int.Parse(Request.Form["Id"].ToString());
                var realName = Request.Form["RealName"].ToString();
                var gender = Request.Form["Gender"].ToString();
                var password = Request.Form["Password"].ToString();

                var user = await _db.TbUsers.FindAsync(id);
                if (user == null) return new JsonResult(new { success = false, message = "用户不存在" });

                user.RealName = realName;
                user.Gender = gender;
                if (!string.IsNullOrWhiteSpace(password)) user.Password = password;

                await _db.SaveChangesAsync();
                return new JsonResult(new { success = true, message = "修改成功" });
            }
            catch (Exception)
            {
                return new JsonResult(new { success = false, message = "服务异常，请稍后重试" });
            }
        }

        public async Task<IActionResult> OnPostDelete(int id)
        {
            try
            {
                var user = await _db.TbUsers.FindAsync(id);
                if (user == null) return new JsonResult(new { success = false, message = "用户不存在" });

                var userRoles = _db.UserRoles.Where(ur => ur.UserId == id).ToList();
                _db.UserRoles.RemoveRange(userRoles);

                _db.TbUsers.Remove(user);
                await _db.SaveChangesAsync();
                return new JsonResult(new { success = true, message = "删除成功" });
            }
            catch (Exception)
            {
                return new JsonResult(new { success = false, message = "服务异常，请稍后重试" });
            }
        }
    }

    public class UserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string CreateTime { get; set; } = string.Empty;
    }
}
