using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using xiaoliran.Data;
using xiaoliran.Models;

namespace xiaoliran.Pages
{
    [IgnoreAntiforgeryToken]
    public class LaundryShopModel : PageModel
    {
        private readonly AppDbContext _db;

        public LaundryShopModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public string SearchKey { get; set; } = string.Empty;

        [BindProperty]
        public string StatusFilter { get; set; } = string.Empty;

        public List<LaundryShop> Shops { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        private const int PageSize = 15;

        public void OnGet(string searchKey = "", string statusFilter = "", int p = 1)
        {
            SearchKey = searchKey;
            StatusFilter = statusFilter;
            var query = _db.LaundryShops.AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchKey))
                query = query.Where(s => s.Name.Contains(searchKey) || s.Address.Contains(searchKey));
            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(s => s.Status == statusFilter);
            var totalCount = query.Count();
            CurrentPage = Math.Max(1, p);
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            Shops = query.OrderByDescending(s => s.CreateTime)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        public async Task<IActionResult> OnPostAdd()
        {
            try
            {
                var name = Request.Form["Name"].ToString();
                var address = Request.Form["Address"].ToString();
                var contactPhone = Request.Form["ContactPhone"].ToString();
                var contactPerson = Request.Form["ContactPerson"].ToString();
                var status = Request.Form["Status"].ToString();
                var businessHours = Request.Form["BusinessHours"].ToString();
                var description = Request.Form["Description"].ToString();

                var shop = new LaundryShop
                {
                    Name = name,
                    Address = address,
                    ContactPhone = contactPhone,
                    ContactPerson = contactPerson,
                    Status = status,
                    BusinessHours = businessHours,
                    Description = description
                };
                _db.LaundryShops.Add(shop);
                await _db.SaveChangesAsync();
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
                var name = Request.Form["Name"].ToString();
                var address = Request.Form["Address"].ToString();
                var contactPhone = Request.Form["ContactPhone"].ToString();
                var contactPerson = Request.Form["ContactPerson"].ToString();
                var status = Request.Form["Status"].ToString();
                var businessHours = Request.Form["BusinessHours"].ToString();
                var description = Request.Form["Description"].ToString();

                var shop = await _db.LaundryShops.FindAsync(id);
                if (shop == null) return new JsonResult(new { success = false, message = "门店不存在" });

                shop.Name = name;
                shop.Address = address;
                shop.ContactPhone = contactPhone;
                shop.ContactPerson = contactPerson;
                shop.Status = status;
                shop.BusinessHours = businessHours;
                shop.Description = description;
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
                var shop = await _db.LaundryShops.FindAsync(id);
                if (shop == null) return new JsonResult(new { success = false, message = "门店不存在" });

                _db.LaundryShops.Remove(shop);
                await _db.SaveChangesAsync();
                return new JsonResult(new { success = true, message = "删除成功" });
            }
            catch (Exception)
            {
                return new JsonResult(new { success = false, message = "服务异常，请稍后重试" });
            }
        }
    }
}
