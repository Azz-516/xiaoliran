using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using xiaoliran.Data;
using xiaoliran.Models;

namespace xiaoliran.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _db;

        public DashboardModel(AppDbContext db)
        {
            _db = db;
        }

        public string ToastMessage { get; set; } = string.Empty;
        public bool IsAdmin => (HttpContext.Session.GetString("UserRoles") ?? "").Contains("admin");

        // Admin stats
        public int UserCount { get; set; }
        public int ShopCount { get; set; }
        public int PendingOrderCount { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<OrderViewModel> RecentOrders { get; set; } = new();

        // User shop grid
        public List<LaundryShop> Shops { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public string SearchKey { get; set; } = string.Empty;
        public string StatusFilter { get; set; } = string.Empty;

        public void OnGet(string searchKey = "", string statusFilter = "")
        {
            ToastMessage = HttpContext.Request.Query["toast"].ToString();

            if (IsAdmin)
            {
                UserCount = _db.TbUsers.Count();
                ShopCount = _db.LaundryShops.Count();
                PendingOrderCount = _db.Orders.Count(o => new[] { "待取件", "待清洗", "洗涤中" }.Contains(o.Status));
                var now = DateTime.Now;
                var monthStart = new DateTime(now.Year, now.Month, 1);
                MonthlyRevenue = _db.Orders
                    .Where(o => o.CreateTime >= monthStart && o.Status == "已送达")
                    .Sum(o => o.EstimatedCost);

                RecentOrders = _db.Orders
                    .OrderByDescending(o => o.CreateTime)
                    .Take(10)
                    .Select(o => new OrderViewModel
                    {
                        OrderNo = o.OrderNo,
                        UserName = _db.TbUsers.Where(u => u.Id == o.UserId).Select(u => u.RealName).FirstOrDefault() ?? "-",
                        ClothingType = o.ClothingType ?? "-",
                        ShopName = _db.LaundryShops.Where(s => s.Id == o.LaundryShopId).Select(s => s.Name).FirstOrDefault() ?? "-",
                        Status = o.Status,
                        CreateTime = o.CreateTime.ToString("yyyy-MM-dd HH:mm")
                    }).ToList();
            }
            else
            {
                const int pageSize = 9;
                SearchKey = searchKey;
                StatusFilter = statusFilter;
                CurrentPage = Math.Max(1, int.TryParse(HttpContext.Request.Query["p"], out var p) ? p : 1);

                var query = _db.LaundryShops.AsQueryable();
                if (!string.IsNullOrWhiteSpace(searchKey))
                {
                    query = query.Where(s => s.Name.Contains(searchKey) || s.Address.Contains(searchKey));
                }
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    query = query.Where(s => s.Status == statusFilter);
                }

                TotalPages = (int)Math.Ceiling(query.Count() / (double)pageSize);
                Shops = query
                    .OrderByDescending(s => s.CreateTime)
                    .Skip((CurrentPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
        }
    }

    public class OrderViewModel
    {
        public string OrderNo { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ClothingType { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreateTime { get; set; } = string.Empty;
    }
}
