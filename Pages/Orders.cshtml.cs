using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using xiaoliran.Data;
using xiaoliran.Models;

namespace xiaoliran.Pages
{
    [IgnoreAntiforgeryToken]
    public class OrdersModel : PageModel
    {
        private readonly AppDbContext _db;

        public OrdersModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public string SearchKey { get; set; } = string.Empty;

        [BindProperty]
        public string StatusFilter { get; set; } = string.Empty;

        public List<OrderAdminViewModel> Orders { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        private const int PageSize = 15;

        public void OnGet(string searchKey = "", string statusFilter = "", int p = 1)
        {
            SearchKey = searchKey;
            StatusFilter = statusFilter;
            var query = _db.Orders.AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchKey))
                query = query.Where(o => o.OrderNo.Contains(searchKey));
            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(o => o.Status == statusFilter);
            var totalCount = query.Count();
            CurrentPage = Math.Max(1, p);
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            Orders = query.OrderByDescending(o => o.CreateTime)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(o => new OrderAdminViewModel
                {
                    Id = o.Id,
                    OrderNo = o.OrderNo,
                    UserName = _db.TbUsers.Where(u => u.Id == o.UserId).Select(u => u.Username).FirstOrDefault() ?? "-",
                    ShopName = _db.LaundryShops.Where(s => s.Id == o.LaundryShopId).Select(s => s.Name).FirstOrDefault() ?? "-",
                    ServiceType = o.ServiceType,
                    ClothingType = o.ClothingType ?? "-",
                    Status = o.Status,
                    EstimatedCost = o.EstimatedCost,
                    Remark = o.Remark ?? "",
                    CreateTime = o.CreateTime.ToString("yyyy-MM-dd HH:mm")
                }).ToList();
        }

        public async Task<IActionResult> OnPostEditStatus()
        {
            try
            {
                var id = int.Parse(Request.Form["Id"].ToString());
                var status = Request.Form["Status"].ToString();

                var order = await _db.Orders.FindAsync(id);
                if (order == null) return new JsonResult(new { success = false, message = "订单不存在" });

                order.Status = status;
                await _db.SaveChangesAsync();
                return new JsonResult(new { success = true, message = "状态修改成功" });
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
                var order = await _db.Orders.FindAsync(id);
                if (order == null) return new JsonResult(new { success = false, message = "订单不存在" });

                _db.Orders.Remove(order);
                await _db.SaveChangesAsync();
                return new JsonResult(new { success = true, message = "删除成功" });
            }
            catch (Exception)
            {
                return new JsonResult(new { success = false, message = "服务异常，请稍后重试" });
            }
        }
    }

    public class OrderAdminViewModel
    {
        public int Id { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string ClothingType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
        public string Remark { get; set; } = string.Empty;
        public string CreateTime { get; set; } = string.Empty;
    }
}
