using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using xiaoliran.Data;
using xiaoliran.Models;

namespace xiaoliran.Pages
{
    public class MyOrdersModel : PageModel
    {
        private readonly AppDbContext _db;

        public MyOrdersModel(AppDbContext db)
        {
            _db = db;
        }

        public List<MyOrderViewModel> Orders { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        private const int PageSize = 15;

        public void OnGet(int p = 1)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                Response.Redirect("/Login");
                return;
            }

            var uid = int.Parse(userId);
            var query = _db.Orders.Where(o => o.UserId == uid);
            var totalCount = query.Count();
            CurrentPage = Math.Max(1, p);
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            Orders = query
                .OrderByDescending(o => o.CreateTime)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(o => new MyOrderViewModel
                {
                    OrderNo = o.OrderNo,
                    ShopName = _db.LaundryShops.Where(s => s.Id == o.LaundryShopId).Select(s => s.Name).FirstOrDefault() ?? "-",
                    ServiceType = o.ServiceType,
                    ClothingType = o.ClothingType ?? "-",
                    Status = o.Status,
                    EstimatedCost = o.EstimatedCost,
                    Remark = o.Remark ?? "-",
                    PickupTime = o.PickupTime.HasValue ? o.PickupTime.Value.ToString("yyyy-MM-dd HH:mm") : "-",
                    DeliveryTime = o.DeliveryTime.HasValue ? o.DeliveryTime.Value.ToString("yyyy-MM-dd HH:mm") : "-",
                    CreateTime = o.CreateTime.ToString("yyyy-MM-dd HH:mm")
                }).ToList();
        }
    }

    public class MyOrderViewModel
    {
        public string OrderNo { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string ClothingType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
        public string Remark { get; set; } = string.Empty;
        public string PickupTime { get; set; } = string.Empty;
        public string DeliveryTime { get; set; } = string.Empty;
        public string CreateTime { get; set; } = string.Empty;
    }
}
