using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using xiaoliran.Data;
using xiaoliran.Models;

namespace xiaoliran.Pages
{
    [IgnoreAntiforgeryToken]
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
        public int ProcessedOrderCount { get; set; }
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
                ProcessedOrderCount = _db.Orders.Count(o => new[] { "已完成", "已送达" }.Contains(o.Status));
                var now = DateTime.Now;
                var monthStart = new DateTime(now.Year, now.Month, 1);
                MonthlyRevenue = _db.Orders
                    .Where(o => o.CreateTime >= monthStart && o.Status == "已送达")
                    .Sum(o => o.EstimatedCost);

                RecentOrders = _db.Orders
                    .OrderByDescending(o => o.CreateTime)
                    .Take(14)
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

        public async Task<IActionResult> OnPostAddOrder()
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                    return new JsonResult(new { success = false, message = "未登录" });

                var laundryShopId = int.Parse(Request.Form["LaundryShopId"]);
                var serviceType = Request.Form["ServiceType"].ToString();
                var clothingType = Request.Form["ClothingType"].ToString();
                var remark = Request.Form["Remark"].ToString();
                var estimatedCostStr = Request.Form["EstimatedCost"].ToString();
                var pickupTimeStr = Request.Form["PickupTime"].ToString();

                if (string.IsNullOrWhiteSpace(clothingType))
                    return new JsonResult(new { success = false, message = "请输入衣物类型" });

                if (!decimal.TryParse(estimatedCostStr, out var estimatedCost) || estimatedCost < 0)
                    return new JsonResult(new { success = false, message = "请输入有效的预估费用" });

                var orderNo = $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";

                var order = new Order
                {
                    OrderNo = orderNo,
                    UserId = int.Parse(userId),
                    LaundryShopId = laundryShopId,
                    ServiceType = serviceType,
                    ClothingType = clothingType,
                    Status = "待取件",
                    EstimatedCost = estimatedCost,
                    Remark = remark,
                    PickupTime = string.IsNullOrWhiteSpace(pickupTimeStr) ? null : DateTime.Parse(pickupTimeStr)
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "下单成功", orderNo = orderNo });
            }
            catch (Exception)
            {
                return new JsonResult(new { success = false, message = "服务异常，请稍后重试" });
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
