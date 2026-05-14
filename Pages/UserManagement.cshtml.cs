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

        public List<RoleItem> AvailableRoles { get; set; } = new();

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
                    CreateTime = u.CreateTime.ToString("yyyy-MM-dd HH:mm"),
                    Phone = u.Phone ?? "",
                    RoleKeys = _db.UserRoles.Where(ur => ur.UserId == u.Id).Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.RoleKey).ToList()
                }).ToList();

            AvailableRoles = _db.Roles.Select(r => new RoleItem { Key = r.RoleKey, Name = r.RoleName }).ToList();
        }

        public async Task<IActionResult> OnPostAdd()
        {
            try
            {
                var username = Request.Form["Username"].ToString();
                var password = Request.Form["Password"].ToString();
                var realName = Request.Form["RealName"].ToString();
                var gender = Request.Form["Gender"].ToString();
                var phone = Request.Form["Phone"].ToString();

                if (await _db.TbUsers.AnyAsync(u => u.Username == username))
                {
                    return new JsonResult(new { success = false, message = "用户名已存在" });
                }

                if (!string.IsNullOrEmpty(phone) && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^1[3-9]\d{9}$"))
                {
                    return new JsonResult(new { success = false, message = "请输入合法的手机号码" });
                }

                var user = new TbUser { Username = username, Password = password, RealName = realName, Gender = gender, Phone = phone };
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
                var phone = Request.Form["Phone"].ToString();
                var password = Request.Form["Password"].ToString();
                var selectedRoles = Request.Form["Roles"].ToArray();

                var user = await _db.TbUsers.FindAsync(id);
                if (user == null) return new JsonResult(new { success = false, message = "用户不存在" });

                user.RealName = realName;
                user.Gender = gender;
                if (!string.IsNullOrEmpty(phone) && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^1[3-9]\d{9}$"))
                {
                    return new JsonResult(new { success = false, message = "请输入合法的手机号码" });
                }
                user.Phone = phone;
                if (!string.IsNullOrWhiteSpace(password)) user.Password = password;

                // Update roles
                var existingUserRoles = _db.UserRoles.Where(ur => ur.UserId == id).ToList();
                _db.UserRoles.RemoveRange(existingUserRoles);

                if (selectedRoles != null && selectedRoles.Length > 0)
                {
                    foreach (var roleKey in selectedRoles)
                    {
                        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleKey == roleKey);
                        if (role != null)
                        {
                            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
                        }
                    }
                }

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
        public string Phone { get; set; } = string.Empty;
        public string CreateTime { get; set; } = string.Empty;
        public List<string> RoleKeys { get; set; } = new();
    }

    public class RoleItem
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
