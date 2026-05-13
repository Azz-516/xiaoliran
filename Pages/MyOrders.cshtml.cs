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

        public void OnGet()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                Response.Redirect("/Login");
                return;
            }

            var uid = int.Parse(userId);
            Orders = _db.Orders
                .Where(o => o.UserId == uid)
                .OrderByDescending(o => o.CreateTime)
                .Select(o => new MyOrderViewModel
                {
                    OrderNo = o.OrderNo,
                    ShopName = _db.LaundryShops.Where(s => s.Id == o.LaundryShopId).Select(s => s.Name).FirstOrDefault() ?? "-",
                    ServiceType = o.ServiceType,
                    ClothingType = o.ClothingType ?? "-",
                    Status = o.Status,
                    EstimatedCost = o.EstimatedCost,
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
        public string CreateTime { get; set; } = string.Empty;
    }
}
