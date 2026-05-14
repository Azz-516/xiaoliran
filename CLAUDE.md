# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**校园干洗店管理系统** (Campus Dry Cleaning Management System) — an ASP.NET Core 10.0 Razor Pages application with Entity Framework Core and SQL Server (LocalDB). Role-based access control (RBAC) for admin vs. regular user experiences.

## Tech Stack

- **.NET 10.0** (`net10.0` in `xiaoliran.csproj`)
- **ASP.NET Core Razor Pages** with Bootstrap 5 + jQuery + jQuery Validation (vendored in `wwwroot/lib/`)
- **Entity Framework Core 10.0.7** (SqlServer provider)
- **Serilog.AspNetCore 10.0.0** — logs to console + `logs/log-YYYYMMDD.txt` (daily rolling, min level Warning, Microsoft overridden to Information)
- **Database**: LocalDB (`cleandb`), Windows auth, connection string hardcoded in `Program.cs`
- **Session-based auth + RBAC**: `AddSession()` + `UseSession()` — stores UserId, UserName, RealName, Gender, UserRoles, UserPermissions after login
- **Namespace**: `xiaoliran` (used in `_ViewImports.cshtml`, all model/page references)

## Build and Run

```bash
dotnet build          # Build the project
dotnet run            # Run the dev server (http://localhost:5079 or https://localhost:7181)
dotnet watch run      # Run with hot reload
```

Dev server ports come from `Properties/launchSettings.json`: HTTP **5079**, HTTPS **7181**.

## Database

- **Auto-created at startup**: `db.Database.EnsureCreated()` then `SeedInitialData(db)` then `SeedTestData(db)` — all called in `Program.cs`.
- **No EF Migrations**: Uses `EnsureCreated()`, not `EnsureMigrated()` or `dotnet ef database update`. Do not add migration files.
- **Connection string**: `Server=(localdb)\MSSQLLocalDB;Database=cleandb;Trusted_Connection=True;TrustServerCertificate=True;` — hardcoded in `Program.cs` line 33.
- **Table name convention**: All tables use `tb_` prefix (configured via `OnModelCreating` → `.ToTable("tb_xxx")`). Do NOT use `[Table]` attribute — use `AppDbContext.OnModelCreating` for mapping.
- **CreateTime**: All entities use `datetime` type with `GETDATE()` default (not `datetime2`).
- **Seeding**: `SeedInitialData` seeds roles (admin, user), permissions, role-permission mappings, and 4 laundry shops. `SeedTestData` creates 50 test users + 50 extra shops (re-run safe via existence checks).
- **Recreate database**: Drop via `sqlcmd -S "(localdb)\MSSQLLocalDB"` with `ALTER DATABASE cleandb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE cleandb;`, then `dotnet run` to recreate.

## Models (`Models/`)

| Model | Table | Purpose |
|-------|-------|---------|
| `TbUser` | `tb_user` | Users (Id, Username, Password, RealName, Gender, CreateTime) |
| `Role` | `tb_role` | RBAC roles (Id, RoleKey, RoleName, Description, CreateTime) |
| `UserRole` | `tb_user_role` | User-Role many-to-many (Id, UserId, RoleId, CreateTime) |
| `Permission` | `tb_permission` | Permissions (Id, PermissionKey, PermissionName, Module, CreateTime) |
| `RolePermission` | `tb_role_permission` | Role-Permission many-to-many (Id, RoleId, PermissionId, CreateTime) |
| `LaundryShop` | `tb_laundry_shop` | Store locations (Id, Name, Address, ContactPhone, ContactPerson, Status, BusinessHours, Description, CreateTime) |
| `Order` | `tb_order` | Orders (Id, OrderNo, UserId, LaundryShopId, ServiceType, ClothingType, Status, EstimatedCost, Remark, PickupTime, DeliveryTime, CreateTime) |
| `PermissionHelper` | — | Static helper: `CheckPermission(PageModel, permissionKey)` — redirects if not authorized |

**Role keys**: `admin`, `user` (comma-separated in session `UserRoles`).
**Permission keys**: `view_dashboard`, `manage_users`, `manage_shops`, `manage_orders`, `view_orders`.

## Architecture

```
Program.cs              # Serilog, DI, EF Core, middleware, /Personal POST, /api/register POST,
                        # SeedInitialData(), SeedTestData()
appsettings.json        # DetailedErrors=true, logging defaults
appsettings.Development.json  # Logging levels, AllowedHosts=*
Data/
  AppDbContext.cs       # EF DbContext — 7 DbSets, OnModelCreating (all table mappings + datetime config)
Models/
  TbUser.cs             # User entity
  Role.cs               # Role entity
  UserRole.cs           # User-Role link
  Permission.cs         # Permission entity
  RolePermission.cs     # Role-Permission link
  LaundryShop.cs        # Laundry shop entity
  Order.cs              # Order entity
  PermissionPageModel.cs # PermissionHelper static class
Pages/
  Shared/
    _AppLayout.cshtml      # Post-login layout: sidebar nav (role-based) + top bar with avatar/profile popup
    _Layout.cshtml         # Pre-login shared layout (Bootstrap navbar/footer)
    _ValidationScriptsPartial.cshtml
  Login.cshtml/.cs        # Login — sets 6 session keys (UserId, UserName, RealName, Gender, UserRoles, UserPermissions)
  Register.cshtml/.cs     # Registration — creates user, assigns default 'user' role, redirects /Login
  Dashboard.cshtml/.cs    # Role-aware: admin = stat cards + recent orders; user = shop card grid (paginated)
  LaundryShop.cshtml/.cs  # Admin: searchable/filterable shop list with AJAX CRUD modals
  UserManagement.cshtml/.cs  # Admin: searchable user list with AJAX CRUD modals
  MyOrders.cshtml/.cs     # User: personal order list (role-gated in _AppLayout sidebar)
  Logout.cshtml/.cs       # POST-only: clears session, redirects to /Login
  Index.cshtml            # Pre-login default home
  Privacy.cshtml / Error.cshtml
  _ViewStart.cshtml       # Default Layout = "_Layout"
  _ViewImports.cshtml     # Imports xiaoliran, xiaoliran.Pages, TagHelpers
Sql/
  Create_tb_user.sql    # Reference SQL script (not used)
wwwroot/
  css/
    site.css          # Default ASP.NET overrides
    login.css         # Login/register gradient theme (#667eea → #764ba2), glassmorphism card
    app.css           # Post-login: sidebar, stat cards, data tables, badges, profile popup, toast, modals, shop grid
  js/site.js            # Empty placeholder
  lib/                  # Bootstrap 5, jQuery, jQuery Validation (vendored)
logs/                   # Serilog log files
Properties/
  launchSettings.json   # HTTP 5079, HTTPS 7181
```

## Key Patterns

- **Two layouts**: Pre-login pages use `_Layout.cshtml` or `Layout = null`. Post-login pages use `_AppLayout.cshtml` with sidebar and `ViewData["Title"]`.
- **Session auth with RBAC**: Login sets 6 session keys including `UserRoles` and `UserPermissions` (comma-separated strings). Sidebar navigation shows/hides items based on `userRoles.Contains("admin")` / `userRoles.Contains("user")`.
- **PermissionHelper**: `PermissionHelper.CheckPermission(page, permissionKey)` checks session for UserId + permission key, redirects to `/Dashboard` with toast if unauthorized.
- **No Personal.cshtml/.cs**: `/Personal` is a minimal API POST endpoint in `Program.cs`. Accepts form data (RealName, Gender, Password), updates user and session, returns JSON `{ success, message, realName, initial, gender }`.
- **/api/register minimal API**: Accepts JSON body (bound to `RegisterRequest` record), creates user + assigns default `user` role, returns JSON.
- **AJAX CRUD pattern**: LaundryShop and UserManagement use `[IgnoreAntiforgeryToken]` on the page model. CRUD operations POST to `?handler=Add`, `?handler=Edit`, `?handler=Delete` with `X-Requested-With: XMLHttpRequest` header. Response: `{ success, message }`.
- **Pagination**: Uses query params `?page=N` with `SearchKey`/`StatusFilter` preserved. `PageSize = 15`. `.card-main` wrapper + `.pagination` div with `.page-link` classes.
- **Modal pattern**: `.modal-overlay` + `.modal-content` with `.show` class toggle. JavaScript functions: `showXxxModal()`, `closeXxxModal()`.
- **Toast UI**: `showToast(msg, success)` in `_AppLayout` — sets `.app-toast` div with `.app-toast-success`/`.app-toast-error` class, auto-dismisses in 3s.
- **Role-aware Dashboard**: `DashboardModel.IsAdmin` checks session `UserRoles.Contains("admin")`. Admin view: 4 stat cards (users, shops, pending orders, monthly revenue) + recent orders table. User view: paginated shop card grid.
- **Status badges**: `.badge-status` with modifiers `pending`, `washing`, `done`, `delivered`. Shop status: `.shop-status-badge` with `status-open` / `status-closed`.
- **Shop cards**: `.shop-grid` with `.shop-card` (`.shop-closed` modifier for closed shops). 3-column grid on desktop.
- **Chinese UI**: All user-facing text is Chinese (zh-CN).
- **No `[Table]` attribute** — all table/column mapping in `AppDbContext.OnModelCreating`. `[Key]` is the only data annotation used for primary keys.
- **Logout is POST-only**: Form submission from _AppLayout sidebar footer.
- **CSS architecture**: `site.css` = default ASP.NET overrides; `login.css` = login/register gradient; `app.css` = post-login components (sidebar, cards, tables, modals, grid). All CSS uses `asp-append-version="true"`.
- **SVG icons**: All icons are inline SVGs (Feather-style).
- **Passwords in plaintext**: No hashing. Direct string comparison.
- **`[IgnoreAntiforgeryToken]`** on pages that use AJAX form submits (UserManagement, LaundryShop) instead of using `@Html.AntiForgeryToken()` in forms.

## Important Notes

- **Git**: branch master.
- **No connection string in appsettings** — hardcoded in `Program.cs`.
- **No `.cursor/`, `.cursorrules`, or `copilot-instructions.md`** files.
- **No `README.md`**.
- **`DateTime.Now`** used in model defaults and seeding — returns local (Beijing) time on Windows server.
- **`RegisterModel.OnPost` redirects to `/Login`** on success — the success toast in Register.cshtml (`IsSuccess == true`) renders only on non-redirect failure paths (though currently success always redirects).
- **`HttpContextAccessor` registered** but not used — session accessed via `HttpContext` property on `PageModel`.
- **`Order.Status` values**: `待取件`, `待清洗`, `洗涤中`, `已完成`, `已送达`.
- **`LaundryShop.Status` values**: `营业中`, `已停业`.
- **`Clothing.cshtml` does not exist** — removed from the project.

## Pagination Gotcha

**CRITICAL**: When adding pagination to Razor Pages, **never use `page` as the query parameter name** — it conflicts with Razor Pages' internal `page` route value.

- ❌ Wrong: `asp-route-page="@i"` or `OnGet(int page = 1)` — overrides the page path
- ✅ Correct: `asp-route-p="@i"` and `OnGet(int p = 1)` — uses query string `?p=2`

All existing pagination (Dashboard, UserManagement, LaundryShop) uses `p` as the parameter name.

## CSS Layout Pattern

Post-login pages use a fixed-height card layout with internal scrolling:

- `.content-area { overflow-y: auto; }` — main scroll container (not body)
- `.card-main { height: 100%; display: flex; flex-direction: column; }` — fills viewport
- `.card-main-body { flex: 1; overflow-x: auto; }` — scrollable content area
- `.card-main-footer { flex-shrink: 0; }` — pagination always visible at bottom
