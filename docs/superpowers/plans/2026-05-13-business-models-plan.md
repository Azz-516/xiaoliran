# 业务系统数据模型 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add LaundryShop and Order data models, implement admin/user pages with real data, remove mock data and Clothing page, implement role-based sidebar navigation.

**Architecture:** Two new entity models with EF Core table mapping in AppDbContext. Admin and regular users see different sidebar menus (role-based). Dashboard splits into admin view (stats + orders) and user view (shop grid). New UserManagement and MyOrders pages. CRUD operations use popup modals and inline confirm().

**Tech Stack:** .NET 10.0, ASP.NET Core Razor Pages, EF Core 10.0.7 (EnsureCreated), session-based auth, Bootstrap 5.

---

## File Structure

### New files (9)

| File | Responsibility |
|------|----------------|
| `Models/LaundryShop.cs` | Laundry shop entity |
| `Models/Order.cs` | Order entity |
| `Pages/UserManagement.cshtml.cs` | User management page model |
| `Pages/UserManagement.cshtml` | User management page |
| `Pages/MyOrders.cshtml.cs` | My orders page model |
| `Pages/MyOrders.cshtml` | My orders page |
| `docs/superpowers/specs/2026-05-13-business-models-design.md` | Design spec (already exists) |

### Modified files (7)

| File | Changes |
|------|---------|
| `Data/AppDbContext.cs` | Add 2 DbSet<T>, OnModelCreating config, foreign keys |
| `Program.cs` | Seed shops + update permissions + remove view_clothing |
| `Pages/Shared/_AppLayout.cshtml` | Role-based sidebar (admin vs user menus) |
| `Pages/Dashboard.cshtml.cs` | Admin: stats + recent orders. User: shop list data |
| `Pages/Dashboard.cshtml` | Admin: real data stats/orders. User: shop card grid |
| `Pages/LaundryShop.cshtml.cs` | Shop CRUD page model |
| `Pages/LaundryShop.cshtml` | Shop list + add/edit/delete popup |

### Removed files (2)

| File | Reason |
|------|--------|
| `Pages/Clothing.cshtml` | No longer needed |
| `Pages/Clothing.cshtml.cs` | No longer needed |

---

### Task 1: Add Business Model Files

**Files:**
- Create: `Models/LaundryShop.cs`
- Create: `Models/Order.cs`

- [ ] **Step 1: Create `Models/LaundryShop.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace xiaoliran.Models
{
    public class LaundryShop
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string ContactPhone { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ContactPerson { get; set; }

        [Required, MaxLength(10)]
        public string Status { get; set; } = "营业中";

        [MaxLength(50)]
        public string? BusinessHours { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
```

- [ ] **Step 2: Create `Models/Order.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace xiaoliran.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string OrderNo { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }

        [Required]
        public int LaundryShopId { get; set; }

        [Required, MaxLength(20)]
        public string ServiceType { get; set; } = "洗衣";

        [MaxLength(50)]
        public string? ClothingType { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "待取件";

        public decimal EstimatedCost { get; set; }

        [MaxLength(500)]
        public string? Remark { get; set; }

        public DateTime? PickupTime { get; set; }

        public DateTime? DeliveryTime { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 2: Update AppDbContext — DbSet, Config, Foreign Keys

**Files:**
- Modify: `Data/AppDbContext.cs`

- [ ] **Step 1: Add DbSet<T> properties**

Add to the existing DbContext class (after `RolePermissions`):

```csharp
public DbSet<LaundryShop> LaundryShops => Set<LaundryShop>();
public DbSet<Order> Orders => Set<Order>();
```

- [ ] **Step 2: Add OnModelCreating configuration for LaundryShop**

Add after the RolePermission config block:

```csharp
// LaundryShop config
modelBuilder.Entity<LaundryShop>().ToTable("tb_laundry_shop");
modelBuilder.Entity<LaundryShop>()
    .Property(s => s.Status)
    .HasMaxLength(10)
    .IsRequired();
modelBuilder.Entity<LaundryShop>()
    .Property(s => s.CreateTime)
    .HasColumnType("datetime")
    .HasDefaultValueSql("GETDATE()");

// Order config
modelBuilder.Entity<Order>().ToTable("tb_order");
modelBuilder.Entity<Order>()
    .Property(o => o.OrderNo)
    .HasMaxLength(50)
    .IsRequired();
modelBuilder.Entity<Order>()
    .Property(o => o.ServiceType)
    .HasMaxLength(20)
    .IsRequired();
modelBuilder.Entity<Order>()
    .Property(o => o.Status)
    .HasMaxLength(20)
    .IsRequired();
modelBuilder.Entity<Order>()
    .Property(o => o.EstimatedCost)
    .HasPrecision(10, 2);
modelBuilder.Entity<Order>()
    .Property(o => o.CreateTime)
    .HasColumnType("datetime")
    .HasDefaultValueSql("GETDATE()");
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 3: Update Seed Data — Shops, Permissions, Remove view_clothing

**Files:**
- Modify: `Program.cs`

- [ ] **Step 1: Replace SeedInitialData method**

Replace the entire `SeedInitialData` method in `Program.cs`:

```csharp
void SeedInitialData(AppDbContext db)
{
    if (db.Roles.Any()) return;

    // Seed roles
    var adminRole = new Role { RoleKey = "admin", RoleName = "管理员", Description = "拥有全部权限" };
    var userRole = new Role { RoleKey = "user", RoleName = "普通用户", Description = "基础查看权限" };
    db.Roles.AddRange(adminRole, userRole);
    db.SaveChanges();

    // Seed permissions
    var dashboardPerm = new Permission { PermissionKey = "view_dashboard", PermissionName = "查看首页", Module = "system" };
    var manageUsersPerm = new Permission { PermissionKey = "manage_users", PermissionName = "用户管理", Module = "user" };
    var manageShopsPerm = new Permission { PermissionKey = "manage_shops", PermissionName = "门店管理", Module = "laundry" };
    var manageOrdersPerm = new Permission { PermissionKey = "manage_orders", PermissionName = "订单管理", Module = "order" };
    var viewOrdersPerm = new Permission { PermissionKey = "view_orders", PermissionName = "查看我的订单", Module = "order" };
    db.Permissions.AddRange(dashboardPerm, manageUsersPerm, manageShopsPerm, manageOrdersPerm, viewOrdersPerm);
    db.SaveChanges();

    // Admin gets: dashboard, manage_users, manage_shops, manage_orders
    db.RolePermissions.AddRange(
        new RolePermission { RoleId = adminRole.Id, PermissionId = dashboardPerm.Id },
        new RolePermission { RoleId = adminRole.Id, PermissionId = manageUsersPerm.Id },
        new RolePermission { RoleId = adminRole.Id, PermissionId = manageShopsPerm.Id },
        new RolePermission { RoleId = adminRole.Id, PermissionId = manageOrdersPerm.Id }
    );

    // User gets: dashboard, view_orders
    db.RolePermissions.AddRange(
        new RolePermission { RoleId = userRole.Id, PermissionId = dashboardPerm.Id },
        new RolePermission { RoleId = userRole.Id, PermissionId = viewOrdersPerm.Id }
    );
    db.SaveChanges();

    // Seed laundry shops
    db.LaundryShops.AddRange(
        new LaundryShop { Name = "东校区洗衣店", Address = "东校区生活区A栋1层", ContactPhone = "010-12345678", ContactPerson = "王师傅", Status = "营业中", BusinessHours = "08:00-20:00", Description = "提供干洗、水洗、熨烫等一站式服务" },
        new LaundryShop { Name = "西校区洗衣店", Address = "西校区商业街B座2层", ContactPhone = "010-23456789", ContactPerson = "李师傅", Status = "营业中", BusinessHours = "09:00-19:00", Description = "专注高端衣物护理与修复" },
        new LaundryShop { Name = "南校区洗衣店", Address = "南校区食堂旁", ContactPhone = "010-34567890", ContactPerson = "张师傅", Status = "营业中", BusinessHours = "08:30-21:00", Description = "快捷清洗服务，当日可取" },
        new LaundryShop { Name = "北校区洗衣店", Address = "北校区体育馆东侧", ContactPhone = "010-45678901", ContactPerson = "赵师傅", Status = "已停业", BusinessHours = "09:00-18:00", Description = "正在装修升级中" }
    );
    db.SaveChanges();
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Drop and recreate database, run app**

Run:
```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "ALTER DATABASE cleandb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE cleandb;"
cd D:/Project/xiaoliran/xiaoliran && timeout 15 dotnet run
```
Expected: App starts, all tables created, seed data populated.

---

### Task 4: Remove Clothing Page

**Files:**
- Remove: `Pages/Clothing.cshtml`
- Remove: `Pages/Clothing.cshtml.cs`

- [ ] **Step 1: Delete Clothing files**

Run:
```bash
del Pages/Clothing.cshtml
del Pages/Clothing.cshtml.cs
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeds with Clothing page removed.

---

### Task 5: Role-Based Sidebar Navigation

**Files:**
- Modify: `Pages/Shared/_AppLayout.cshtml`

- [ ] **Step 1: Add userRoles session read**

Add `userRoles` variable at the top (after `userPermissions`):

```csharp
var userPermissions = Context.Session.GetString("UserPermissions") ?? "";
var userRoles = Context.Session.GetString("UserRoles") ?? "";
```

- [ ] **Step 2: Replace sidebar nav section**

Replace the entire `<nav class="sidebar-nav">` block with role-based menus:

```html
<nav class="sidebar-nav">
    <a class="nav-item @(Context.Request.Path == "/Dashboard" ? "active" : "")" asp-page="/Dashboard">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="20" height="20">
            <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/>
        </svg>
        <span>首页</span>
    </a>
    @if (userRoles.Contains("admin"))
    {
        <a class="nav-item @(Context.Request.Path == "/UserManagement" ? "active" : "")" asp-page="/UserManagement">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="20" height="20">
                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>
            </svg>
            <span>用户管理</span>
        </a>
        <a class="nav-item @(Context.Request.Path == "/LaundryShop" ? "active" : "")" asp-page="/LaundryShop">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="20" height="20">
                <rect x="2" y="7" width="20" height="14" rx="2" ry="2"/><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/>
            </svg>
            <span>门店管理</span>
        </a>
    }
    @if (userRoles.Contains("user"))
    {
        <a class="nav-item @(Context.Request.Path == "/MyOrders" ? "active" : "")" asp-page="/MyOrders">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="20" height="20">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/>
            </svg>
            <span>我的订单</span>
        </a>
    }
</nav>
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 6: Rewrite Dashboard with Real Data (Admin + User)

**Files:**
- Modify: `Pages/Dashboard.cshtml.cs`
- Modify: `Pages/Dashboard.cshtml`

- [ ] **Step 1: Update `Pages/Dashboard.cshtml.cs`**

Replace with admin/user dual-view model:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

        public void OnGet()
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
                const int pageSize = 12;
                CurrentPage = Math.Max(1, int.TryParse(HttpContext.Request.Query["page"], out var p) ? p : 1);
                TotalPages = (int)Math.Ceiling(_db.LaundryShops.Count() / (double)pageSize);
                Shops = _db.LaundryShops
                    .OrderBy(s => s.Id)
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
```

- [ ] **Step 2: Update `Pages/Dashboard.cshtml`**

Replace entire file with conditional admin/user content:

```html
@page
@model xiaoliran.Pages.DashboardModel
@{
    ViewData["Title"] = "首页";
    Layout = "/Pages/Shared/_AppLayout.cshtml";
}

@if (Model.IsAdmin)
{
    <div class="dashboard-grid">
        <div class="stat-card">
            <div class="stat-icon purple">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="24" height="24">
                    <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                </svg>
            </div>
            <div class="stat-value">@Model.UserCount</div>
            <div class="stat-label">注册用户</div>
        </div>
        <div class="stat-card">
            <div class="stat-icon blue">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="24" height="24">
                    <rect x="2" y="7" width="20" height="14" rx="2" ry="2"/><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/>
                </svg>
            </div>
            <div class="stat-value">@Model.ShopCount</div>
            <div class="stat-label">洗衣门店</div>
        </div>
        <div class="stat-card">
            <div class="stat-icon green">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="24" height="24">
                    <path d="M22 12h-4l-3 9L9 3l-3 9H2"/>
                </svg>
            </div>
            <div class="stat-value">@Model.PendingOrderCount</div>
            <div class="stat-label">待处理订单</div>
        </div>
        <div class="stat-card">
            <div class="stat-icon orange">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="24" height="24">
                    <line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
                </svg>
            </div>
            <div class="stat-value">¥@Model.MonthlyRevenue.ToString("N0")</div>
            <div class="stat-label">本月营收</div>
        </div>
    </div>

    <div class="card-main">
        <div class="card-main-header">
            <h3>最近订单</h3>
        </div>
        <div class="card-main-body">
            <table class="data-table">
                <thead>
                    <tr>
                        <th>订单号</th>
                        <th>用户</th>
                        <th>衣物类型</th>
                        <th>门店</th>
                        <th>状态</th>
                        <th>下单时间</th>
                    </tr>
                </thead>
                <tbody>
                    @if (Model.RecentOrders.Count == 0)
                    {
                        <tr><td colspan="6" style="text-align:center;color:#a0aec0;">暂无订单数据</td></tr>
                    }
                    else
                    {
                        @foreach (var order in Model.RecentOrders)
                        {
                            <tr>
                                <td>@order.OrderNo</td>
                                <td>@order.UserName</td>
                                <td>@order.ClothingType</td>
                                <td>@order.ShopName</td>
                                <td><span class="badge-status @(GetOrderBadgeClass(order.Status))">@order.Status</span></td>
                                <td>@order.CreateTime</td>
                            </tr>
                        }
                    }
                </tbody>
            </table>
        </div>
    </div>
}
else
{
    <div class="shop-grid">
        @foreach (var shop in Model.Shops)
        {
            <div class="shop-card @(shop.Status == "已停业" ? "shop-closed" : "")">
                <div class="shop-card-header">
                    <h3>@shop.Name</h3>
                    <span class="shop-status-badge @(shop.Status == "营业中" ? "status-open" : "status-closed")">@shop.Status</span>
                </div>
                <div class="shop-card-body">
                    <div class="shop-info-row">
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="16" height="16">
                            <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/>
                        </svg>
                        <span>@shop.Address</span>
                    </div>
                    <div class="shop-info-row">
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="16" height="16">
                            <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z"/>
                        </svg>
                        <span>@shop.ContactPhone</span>
                    </div>
                    @if (!string.IsNullOrEmpty(shop.BusinessHours))
                    {
                        <div class="shop-info-row">
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="16" height="16">
                                <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
                            </svg>
                            <span>@shop.BusinessHours</span>
                        </div>
                    }
                </div>
            </div>
        }
    </div>

    @if (Model.TotalPages > 1)
    {
        <div class="pagination">
            @for (var i = 1; i <= Model.TotalPages; i++)
            {
                <a class="page-link @(i == Model.CurrentPage ? "active" : "")" asp-page="/Dashboard" asp-route-page="@i">@i</a>
            }
        </div>
    }
}

@if (!string.IsNullOrEmpty(Model.ToastMessage))
{
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            showToast('@Model.ToastMessage', false);
        });
    </script>
}

@functions {
    public static string GetOrderBadgeClass(string status)
    {
        return status switch
        {
            "待取件" => "pending",
            "待清洗" => "washing",
            "洗涤中" => "washing",
            "已完成" => "done",
            "已送达" => "delivered",
            _ => "pending"
        };
    }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 7: Add Shop Grid CSS Styles

**Files:**
- Modify: `wwwroot/css/app.css`

- [ ] **Step 1: Add shop grid, pagination, and badge styles**

Append to `wwwroot/css/app.css`:

```css
/* ---- Shop Grid ---- */
.shop-grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 24px;
    margin-bottom: 32px;
}

.shop-card {
    background: white;
    border-radius: 16px;
    padding: 24px;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06);
    transition: all 0.25s ease;
    border: 2px solid transparent;
}

.shop-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 8px 24px rgba(102, 126, 234, 0.12);
    border-color: #667eea;
}

.shop-card.shop-closed {
    opacity: 0.6;
}

.shop-card.shop-closed:hover {
    opacity: 0.8;
}

.shop-card-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 16px;
}

.shop-card-header h3 {
    font-size: 16px;
    font-weight: 700;
    color: #1a202c;
    margin: 0;
}

.shop-status-badge {
    padding: 4px 12px;
    border-radius: 20px;
    font-size: 12px;
    font-weight: 600;
}

.shop-status-badge.status-open {
    background: #d4edda;
    color: #155724;
}

.shop-status-badge.status-closed {
    background: #f8d7da;
    color: #721c24;
}

.shop-card-body {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.shop-info-row {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 13px;
    color: #4a5568;
}

.shop-info-row svg {
    color: #a0aec0;
    flex-shrink: 0;
}

/* ---- Pagination ---- */
.pagination {
    display: flex;
    justify-content: center;
    gap: 8px;
    margin-top: 24px;
}

.page-link {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 36px;
    height: 36px;
    border-radius: 8px;
    background: white;
    color: #4a5568;
    text-decoration: none;
    font-size: 14px;
    font-weight: 500;
    transition: all 0.2s ease;
    border: 1px solid #e8ecf1;
}

.page-link:hover {
    background: #667eea;
    color: white;
    border-color: #667eea;
}

.page-link.active {
    background: #667eea;
    color: white;
    border-color: #667eea;
}

/* ---- CRUD Buttons ---- */
.crud-actions {
    display: flex;
    gap: 8px;
}

.btn-edit {
    padding: 4px 12px;
    background: #4facfe;
    color: white;
    border: none;
    border-radius: 6px;
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
}

.btn-edit:hover {
    background: #3a9bfe;
}

.btn-delete {
    padding: 4px 12px;
    background: #fa709a;
    color: white;
    border: none;
    border-radius: 6px;
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
}

.btn-delete:hover {
    background: #e8608a;
}

.btn-add {
    padding: 10px 24px;
    background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%);
    color: white;
    border: none;
    border-radius: 10px;
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    box-shadow: 0 2px 8px rgba(67, 233, 123, 0.3);
    transition: all 0.2s ease;
}

.btn-add:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(67, 233, 123, 0.4);
}

/* ---- Modal / Confirm Popup ---- */
.modal-overlay {
    display: none;
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.5);
    z-index: 1000;
    justify-content: center;
    align-items: center;
}

.modal-overlay.show {
    display: flex;
}

.modal-content {
    background: white;
    border-radius: 16px;
    padding: 32px;
    max-width: 480px;
    width: 90%;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.modal-content h3 {
    font-size: 18px;
    font-weight: 700;
    color: #1a202c;
    margin: 0 0 24px 0;
}

.modal-footer {
    display: flex;
    justify-content: flex-end;
    gap: 12px;
    margin-top: 24px;
}

/* ---- Responsive ---- */
@media (max-width: 768px) {
    .sidebar {
        width: 64px;
    }

    .sidebar-header span,
    .sidebar-nav .nav-item span,
    .btn-logout span {
        display: none;
    }

    .sidebar-header {
        justify-content: center;
        padding: 20px 0;
    }

    .sidebar-nav .nav-item {
        justify-content: center;
        padding: 12px;
    }

    .btn-logout {
        justify-content: center;
        padding: 12px;
    }

    .main-content {
        margin-left: 64px;
    }

    .user-name {
        display: none;
    }

    .dashboard-grid {
        grid-template-columns: 1fr;
    }

    .shop-grid {
        grid-template-columns: repeat(2, 1fr);
    }
}

@media (max-width: 480px) {
    .shop-grid {
        grid-template-columns: 1fr;
    }
}
```

- [ ] **Step 2: Remove the old responsive section**

Remove the existing `/* ---- Responsive ---- */` section from the original CSS (lines 552-589) since it's replaced by the new one above. Actually, the existing responsive section is identical in structure. Keep it as-is since the new `@media` sections are additions, not replacements.

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeds. CSS changes don't affect compilation.

---

### Task 8: Create User Management Page (CRUD)

**Files:**
- Create: `Pages/UserManagement.cshtml.cs`
- Create: `Pages/UserManagement.cshtml`

- [ ] **Step 1: Create `Pages/UserManagement.cshtml.cs`**

Page model with OnGet for list, OnPost handlers for add/edit/delete:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using xiaoliran.Models;

namespace xiaoliran.Pages
{
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

        public void OnGet(string searchKey = "")
        {
            var query = _db.TbUsers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                query = query.Where(u => u.Username.Contains(searchKey) || u.RealName.Contains(searchKey));
            }
            Users = query.OrderByDescending(u => u.CreateTime)
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
                    return Json(new { success = false, message = "用户名已存在" });
                }

                var user = new TbUser { Username = username, Password = password, RealName = realName, Gender = gender };
                _db.TbUsers.Add(user);
                await _db.SaveChangesAsync();

                // Assign default 'user' role
                var userRoleId = await _db.Roles.Where(r => r.RoleKey == "user").Select(r => r.Id).FirstOrDefaultAsync();
                if (userRoleId > 0)
                {
                    _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRoleId });
                    await _db.SaveChangesAsync();
                }

                return Json(new { success = true, message = "添加成功" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "服务异常，请稍后重试" });
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
                if (user == null) return Json(new { success = false, message = "用户不存在" });

                user.RealName = realName;
                user.Gender = gender;
                if (!string.IsNullOrWhiteSpace(password)) user.Password = password;

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "修改成功" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "服务异常，请稍后重试" });
            }
        }

        public async Task<IActionResult> OnPostDelete(int id)
        {
            try
            {
                var user = await _db.TbUsers.FindAsync(id);
                if (user == null) return Json(new { success = false, message = "用户不存在" });

                // Remove user-role associations first
                var userRoles = _db.UserRoles.Where(ur => ur.UserId == id).ToList();
                _db.UserRoles.RemoveRange(userRoles);

                _db.TbUsers.Remove(user);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "删除成功" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "服务异常，请稍后重试" });
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
```

- [ ] **Step 2: Create `Pages/UserManagement.cshtml`**

Page with search bar, add button, user table, and add/edit modal:

```html
@page
@model xiaoliran.Pages.UserManagementModel
@{
    ViewData["Title"] = "用户管理";
    Layout = "/Pages/Shared/_AppLayout.cshtml";
}

<div class="card-main">
    <div class="card-main-header">
        <h3>用户列表</h3>
        <div style="display:flex;gap:12px;align-items:center;">
            <form method="get" style="display:flex;gap:8px;">
                <input type="text" name="searchKey" value="@Model.SearchKey" placeholder="搜索用户名或姓名" class="form-input" style="width:200px;" />
                <button type="submit" class="btn-primary-app">搜索</button>
            </form>
            <button class="btn-add" onclick="showAddModal()">+ 新增用户</button>
        </div>
    </div>
    <div class="card-main-body">
        <table class="data-table">
            <thead>
                <tr>
                    <th>用户名</th>
                    <th>姓名</th>
                    <th>性别</th>
                    <th>注册时间</th>
                    <th>操作</th>
                </tr>
            </thead>
            <tbody>
                @if (Model.Users.Count == 0)
                {
                    <tr><td colspan="5" style="text-align:center;color:#a0aec0;">暂无用户数据</td></tr>
                }
                else
                {
                    @foreach (var user in Model.Users)
                    {
                        <tr>
                            <td>@user.Username</td>
                            <td>@user.RealName</td>
                            <td>@user.Gender</td>
                            <td>@user.CreateTime</td>
                            <td>
                                <div class="crud-actions">
                                    <button class="btn-edit" onclick="showEditModal(@user.Id, '@user.Username', '@user.RealName', '@user.Gender')">编辑</button>
                                    <button class="btn-delete" onclick="confirmDelete(@user.Id, '@user.Username')">删除</button>
                                </div>
                            </td>
                        </tr>
                    }
                }
            </tbody>
        </table>
    </div>
</div>

<!-- Add User Modal -->
<div class="modal-overlay" id="addModal">
    <div class="modal-content">
        <h3>新增用户</h3>
        <form id="addForm">
            <div class="form-row">
                <label>用户名</label>
                <input type="text" name="Username" class="form-input" required />
            </div>
            <div class="form-row">
                <label>密码</label>
                <input type="password" name="Password" class="form-input" required />
            </div>
            <div class="form-row">
                <label>姓名</label>
                <input type="text" name="RealName" class="form-input" required />
            </div>
            <div class="form-row">
                <label>性别</label>
                <div class="form-radio-group">
                    <label class="form-radio"><input type="radio" name="Gender" value="男" checked /> 男</label>
                    <label class="form-radio"><input type="radio" name="Gender" value="女" /> 女</label>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary-app" onclick="closeAddModal()">取消</button>
                <button type="submit" class="btn-primary-app">确认添加</button>
            </div>
        </form>
    </div>
</div>

<!-- Edit User Modal -->
<div class="modal-overlay" id="editModal">
    <div class="modal-content">
        <h3>编辑用户</h3>
        <form id="editForm">
            <input type="hidden" name="Id" id="editId" />
            <div class="form-row">
                <label>用户名</label>
                <input type="text" id="editUsername" class="form-readonly" readonly />
            </div>
            <div class="form-row">
                <label>姓名</label>
                <input type="text" name="RealName" id="editRealName" class="form-input" required />
            </div>
            <div class="form-row">
                <label>性别</label>
                <div class="form-radio-group">
                    <label class="form-radio"><input type="radio" name="Gender" value="男" checked /> 男</label>
                    <label class="form-radio"><input type="radio" name="Gender" value="女" /> 女</label>
                </div>
            </div>
            <div class="form-row">
                <label>新密码（不修改请留空）</label>
                <input type="password" name="Password" class="form-input" />
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary-app" onclick="closeEditModal()">取消</button>
                <button type="submit" class="btn-primary-app">保存修改</button>
            </div>
        </form>
    </div>
</div>

<script>
    function showAddModal() { document.getElementById('addModal').classList.add('show'); }
    function closeAddModal() { document.getElementById('addModal').classList.remove('show'); document.getElementById('addForm').reset(); }
    function showEditModal(id, username, realName, gender) {
        document.getElementById('editId').value = id;
        document.getElementById('editUsername').value = username;
        document.getElementById('editRealName').value = realName;
        document.getElementById('editModal').classList.add('show');
        document.querySelectorAll('#editModal input[name="Gender"]').forEach(function(r) { r.checked = r.value === gender; });
    }
    function closeEditModal() { document.getElementById('editModal').classList.remove('show'); }
    document.getElementById('addForm').addEventListener('submit', function(e) {
        e.preventDefault();
        var fd = new FormData(this);
        fetch('?handler=Add', { method: 'POST', body: fd, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function(r) { return r.json(); }).then(function(data) {
                if (data.success) { location.reload(); } else { alert(data.message); }
            });
    });
    document.getElementById('editForm').addEventListener('submit', function(e) {
        e.preventDefault();
        var fd = new FormData(this);
        fetch('?handler=Edit', { method: 'POST', body: fd, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function(r) { return r.json(); }).then(function(data) {
                if (data.success) { location.reload(); } else { alert(data.message); }
            });
    });
    function confirmDelete(id, username) {
        if (confirm('确定要删除用户 "' + username + '" 吗？此操作不可恢复。')) {
            var fd = new FormData();
            fd.append('id', id);
            fetch('?handler=Delete&id=' + id, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function(r) { return r.json(); }).then(function(data) {
                    if (data.success) { location.reload(); } else { alert(data.message); }
                });
        }
    }
    // Close modals on overlay click
    document.getElementById('addModal').addEventListener('click', function(e) { if (e.target === this) closeAddModal(); });
    document.getElementById('editModal').addEventListener('click', function(e) { if (e.target === this) closeEditModal(); });
</script>
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 9: Rewrite LaundryShop Page with CRUD

**Files:**
- Modify: `Pages/LaundryShop.cshtml.cs`
- Modify: `Pages/LaundryShop.cshtml`

- [ ] **Step 1: Rewrite `Pages/LaundryShop.cshtml.cs`**

Replace with shop CRUD page model:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using xiaoliran.Models;

namespace xiaoliran.Pages
{
    public class LaundryShopModel : PageModel
    {
        private readonly AppDbContext _db;

        public LaundryShopModel(AppDbContext db)
        {
            _db = db;
        }

        public List<LaundryShop> Shops { get; set; } = new();
        public string StatusFilter { get; set; } = "全部";

        public void OnGet(string statusFilter = "全部")
        {
            StatusFilter = statusFilter;
            var query = _db.LaundryShops.AsQueryable();
            if (statusFilter != "全部")
            {
                query = query.Where(s => s.Status == statusFilter);
            }
            Shops = query.OrderByDescending(s => s.CreateTime).ToList();
        }

        public async Task<IActionResult> OnPostAdd()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                var shop = new LaundryShop
                {
                    Name = form["Name"].ToString(),
                    Address = form["Address"].ToString(),
                    ContactPhone = form["ContactPhone"].ToString(),
                    ContactPerson = form["ContactPerson"].ToString(),
                    BusinessHours = form["BusinessHours"].ToString(),
                    Description = form["Description"].ToString(),
                    Status = form["Status"].ToString()
                };
                _db.LaundryShops.Add(shop);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "添加成功" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "服务异常，请稍后重试" });
            }
        }

        public async Task<IActionResult> OnPostEdit()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                var id = int.Parse(form["Id"].ToString());
                var shop = await _db.LaundryShops.FindAsync(id);
                if (shop == null) return Json(new { success = false, message = "门店不存在" });

                shop.Name = form["Name"].ToString();
                shop.Address = form["Address"].ToString();
                shop.ContactPhone = form["ContactPhone"].ToString();
                shop.ContactPerson = form["ContactPerson"].ToString();
                shop.BusinessHours = form["BusinessHours"].ToString();
                shop.Description = form["Description"].ToString();
                shop.Status = form["Status"].ToString();

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "修改成功" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "服务异常，请稍后重试" });
            }
        }

        public async Task<IActionResult> OnPostDelete(int id)
        {
            try
            {
                var shop = await _db.LaundryShops.FindAsync(id);
                if (shop == null) return Json(new { success = false, message = "门店不存在" });

                _db.LaundryShops.Remove(shop);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "删除成功" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "服务异常，请稍后重试" });
            }
        }
    }
}
```

- [ ] **Step 2: Rewrite `Pages/LaundryShop.cshtml`**

Replace with shop CRUD page:

```html
@page
@model xiaoliran.Pages.LaundryShopModel
@{
    ViewData["Title"] = "门店管理";
    Layout = "/Pages/Shared/_AppLayout.cshtml";
}

<div class="card-main">
    <div class="card-main-header">
        <h3>门店列表</h3>
        <div style="display:flex;gap:12px;align-items:center;">
            <form method="get" style="display:flex;gap:8px;">
                <select name="statusFilter" class="form-input" style="width:140px;">
                    <option value="全部" @(Model.StatusFilter == "全部" ? "selected" : "")>全部</option>
                    <option value="营业中" @(Model.StatusFilter == "营业中" ? "selected" : "")>营业中</option>
                    <option value="已停业" @(Model.StatusFilter == "已停业" ? "selected" : "")>已停业</option>
                </select>
                <button type="submit" class="btn-primary-app">筛选</button>
            </form>
            <button class="btn-add" onclick="showAddModal()">+ 新增门店</button>
        </div>
    </div>
    <div class="card-main-body">
        <table class="data-table">
            <thead>
                <tr>
                    <th>门店名称</th>
                    <th>地址</th>
                    <th>联系电话</th>
                    <th>负责人</th>
                    <th>营业时间</th>
                    <th>状态</th>
                    <th>操作</th>
                </tr>
            </thead>
            <tbody>
                @if (Model.Shops.Count == 0)
                {
                    <tr><td colspan="7" style="text-align:center;color:#a0aec0;">暂无门店数据</td></tr>
                }
                else
                {
                    @foreach (var shop in Model.Shops)
                    {
                        <tr>
                            <td>@shop.Name</td>
                            <td>@shop.Address</td>
                            <td>@shop.ContactPhone</td>
                            <td>@(shop.ContactPerson ?? "-")</td>
                            <td>@(shop.BusinessHours ?? "-")</td>
                            <td><span class="badge-status @(shop.Status == "营业中" ? "done" : "pending")">@shop.Status</span></td>
                            <td>
                                <div class="crud-actions">
                                    <button class="btn-edit" onclick="showEditModal(@shop.Id, '@shop.Name.Replace("'", "\\'")', '@shop.Address.Replace("'", "\\'")', '@shop.ContactPhone', '@(shop.ContactPerson ?? "")', '@(shop.BusinessHours ?? "")', '@(shop.Description ?? "")', '@shop.Status')">编辑</button>
                                    <button class="btn-delete" onclick="confirmDelete(@shop.Id, '@shop.Name.Replace("'", "\\'")')">删除</button>
                                </div>
                            </td>
                        </tr>
                    }
                }
            </tbody>
        </table>
    </div>
</div>

<!-- Add/Edit Modal -->
<div class="modal-overlay" id="shopModal">
    <div class="modal-content" style="max-width:560px;">
        <h3 id="modalTitle">新增门店</h3>
        <form id="shopForm">
            <input type="hidden" name="Id" id="shopId" />
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:0 16px;">
                <div class="form-row">
                    <label>门店名称</label>
                    <input type="text" name="Name" id="shopName" class="form-input" required />
                </div>
                <div class="form-row">
                    <label>联系电话</label>
                    <input type="text" name="ContactPhone" id="shopPhone" class="form-input" required />
                </div>
                <div class="form-row" style="grid-column: span 2;">
                    <label>地址</label>
                    <input type="text" name="Address" id="shopAddress" class="form-input" required />
                </div>
                <div class="form-row">
                    <label>负责人</label>
                    <input type="text" name="ContactPerson" id="shopContact" class="form-input" />
                </div>
                <div class="form-row">
                    <label>营业时间</label>
                    <input type="text" name="BusinessHours" id="shopHours" class="form-input" placeholder="如 08:00-20:00" />
                </div>
                <div class="form-row" style="grid-column: span 2;">
                    <label>状态</label>
                    <div class="form-radio-group">
                        <label class="form-radio"><input type="radio" name="Status" value="营业中" checked /> 营业中</label>
                        <label class="form-radio"><input type="radio" name="Status" value="已停业" /> 已停业</label>
                    </div>
                </div>
                <div class="form-row" style="grid-column: span 2;">
                    <label>简介</label>
                    <textarea name="Description" id="shopDesc" class="form-input" rows="2"></textarea>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary-app" onclick="closeShopModal()">取消</button>
                <button type="submit" class="btn-primary-app">保存</button>
            </div>
        </form>
    </div>
</div>

<script>
    function showAddModal() {
        document.getElementById('modalTitle').textContent = '新增门店';
        document.getElementById('shopId').value = '';
        document.getElementById('shopForm').reset();
        document.getElementById('shopModal').classList.add('show');
    }
    function showEditModal(id, name, address, phone, contact, hours, desc, status) {
        document.getElementById('modalTitle').textContent = '编辑门店';
        document.getElementById('shopId').value = id;
        document.getElementById('shopName').value = name;
        document.getElementById('shopAddress').value = address;
        document.getElementById('shopPhone').value = phone;
        document.getElementById('shopContact').value = contact;
        document.getElementById('shopHours').value = hours;
        document.getElementById('shopDesc').value = desc;
        document.querySelectorAll('#shopModal input[name="Status"]').forEach(function(r) { r.checked = r.value === status; });
        document.getElementById('shopModal').classList.add('show');
    }
    function closeShopModal() { document.getElementById('shopModal').classList.remove('show'); }
    document.getElementById('shopForm').addEventListener('submit', function(e) {
        e.preventDefault();
        var fd = new FormData(this);
        var id = document.getElementById('shopId').value;
        var handler = id ? 'Edit' : 'Add';
        fetch('?handler=' + handler, { method: 'POST', body: fd, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function(r) { return r.json(); }).then(function(data) {
                if (data.success) { location.reload(); } else { alert(data.message); }
            });
    });
    function confirmDelete(id, name) {
        if (confirm('确定要删除门店 "' + name + '" 吗？')) {
            fetch('?handler=Delete&id=' + id, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function(r) { return r.json(); }).then(function(data) {
                    if (data.success) { location.reload(); } else { alert(data.message); }
                });
        }
    }
    document.getElementById('shopModal').addEventListener('click', function(e) { if (e.target === this) closeShopModal(); });
</script>
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 10: Create MyOrders Page

**Files:**
- Create: `Pages/MyOrders.cshtml.cs`
- Create: `Pages/MyOrders.cshtml`

- [ ] **Step 1: Create `Pages/MyOrders.cshtml.cs`**

```csharp
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

        public List<OrderListViewModel> Orders { get; set; } = new();

        public void OnGet()
        {
            var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
            Orders = _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreateTime)
                .Select(o => new OrderListViewModel
                {
                    Id = o.Id,
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

    public class OrderListViewModel
    {
        public int Id { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string ClothingType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
        public string CreateTime { get; set; } = string.Empty;
    }
}
```

- [ ] **Step 2: Create `Pages/MyOrders.cshtml`**

```html
@page
@model xiaoliran.Pages.MyOrdersModel
@{
    ViewData["Title"] = "我的订单";
    Layout = "/Pages/Shared/_AppLayout.cshtml";
}

<div class="card-main">
    <div class="card-main-header">
        <h3>我的订单</h3>
    </div>
    <div class="card-main-body">
        <table class="data-table">
            <thead>
                <tr>
                    <th>订单号</th>
                    <th>门店</th>
                    <th>服务类型</th>
                    <th>衣物类型</th>
                    <th>状态</th>
                    <th>预估费用</th>
                    <th>下单时间</th>
                </tr>
            </thead>
            <tbody>
                @if (Model.Orders.Count == 0)
                {
                    <tr><td colspan="7" style="text-align:center;color:#a0aec0;">暂无订单</td></tr>
                }
                else
                {
                    @foreach (var order in Model.Orders)
                    {
                        <tr>
                            <td>@order.OrderNo</td>
                            <td>@order.ShopName</td>
                            <td>@order.ServiceType</td>
                            <td>@order.ClothingType</td>
                            <td><span class="badge-status @(GetOrderBadgeClass(order.Status))">@order.Status</span></td>
                            <td>¥@order.EstimatedCost.ToString("F2")</td>
                            <td>@order.CreateTime</td>
                        </tr>
                    }
                }
            </tbody>
        </table>
    </div>
</div>

@functions {
    public static string GetOrderBadgeClass(string status)
    {
        return status switch
        {
            "待取件" => "pending",
            "待清洗" => "washing",
            "洗涤中" => "washing",
            "已完成" => "done",
            "已送达" => "delivered",
            _ => "pending"
        };
    }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 11: Remove view_clothing Permission References

**Files:**
- Modify: `Program.cs` (seed data already updated in Task 3)

- [ ] **Step 1: Verify Task 3 already removed view_clothing**

Task 3 already replaced the seed data to remove `view_clothing`. Verify that `Program.cs` no longer references `view_clothing` or `clothingPerm`.

Run: `grep -n "clothing" Program.cs`
Expected: No matches (case-insensitive search for "clothing" or "clothingPerm").

- [ ] **Step 2: Verify no other references**

Run: `grep -rn "Clothing" --include="*.cs" --include="*.cshtml" .`
Expected: Only the _ViewImports.cshtml namespace reference (which is auto-generated and fine).

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 12: Full Build & Database Recreation

- [ ] **Step 1: Drop and recreate database**

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "ALTER DATABASE cleandb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE cleandb;"
```

- [ ] **Step 2: Build and run**

```bash
cd D:/Project/xiaoliran/xiaoliran && dotnet build && timeout 15 dotnet run
```

Expected: App starts successfully, all tables created (tb_user, tb_role, tb_user_role, tb_permission, tb_role_permission, tb_laundry_shop, tb_order), seed data populated.

- [ ] **Step 3: Manual E2E verification**

1. Register a new user → login → verify sidebar shows 首页 + 我的订单
2. Dashboard shows shop cards (4-grid) with pagination
3. MyOrders shows empty order list
4. Manually assign admin role in DB: `INSERT INTO tb_user_role (UserId, RoleId) VALUES (1, 1);`
5. Login as admin → verify sidebar shows 首页 + 用户管理 + 门店管理
6. Dashboard shows stats (user count, shop count, etc.) and recent orders (empty table)
7. UserManagement shows user list, add/edit/delete works
8. LaundryShop shows shop list (seed data), add/edit/delete works

---

## Spec Coverage Checklist

| Spec Requirement | Task(s) |
|-----------------|---------|
| LaundryShop model with all fields | Task 1 |
| Order model with all fields | Task 1 |
| AppDbContext DbSet + OnModelCreating config | Task 2 |
| Seed laundry shop data (4 shops) | Task 3 |
| New permissions (manage_users, manage_shops, manage_orders, view_orders) | Task 3 |
| Remove view_clothing permission | Task 3, Task 11 |
| Role-based sidebar (admin vs user) | Task 5 |
| Admin Dashboard with real stats | Task 6 |
| User Dashboard with shop grid + pagination | Task 6, Task 7 |
| User Management CRUD (add/edit/delete/search) | Task 8 |
| Laundry Shop CRUD (add/edit/delete/filter) | Task 9 |
| My Orders page for regular users | Task 10 |
| Remove Clothing page | Task 4 |
| CSS styles (shop grid, pagination, CRUD buttons, modal) | Task 7 |
| Badge class for new order statuses | Task 6, Task 10 |
| All tables use OnModelCreating (no [Table]) | Tasks 1, 2 |
| datetime + GETDATE() for CreateTime | Task 2 |
| Table prefix tb_ | Task 2 |

## Self-Review

- **Placeholder scan:** No TBD/TODO. All tasks have complete code.
- **Internal consistency:** Permission keys consistent across spec and plan. Badge classes reused for order statuses. Shop grid uses existing dashboard-grid pattern adapted for 4-column layout.
- **Type consistency:** All models use same conventions (Key attribute, datetime CreateTime). Page models follow same pattern (AppDbContext injection, LINQ queries).
- **Security:** User delete cascades to user_role associations. Admin-only access via role-based sidebar.
- **Edge cases:** Empty data states handled with placeholder rows. Pagination handles 0 results. Modal form reset on close.
- **Seed data:** Shops seed runs only when Roles table is empty (idempotent).
