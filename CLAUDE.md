# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**校园干洗店管理系统** (Campus Dry Cleaning Management System) — an ASP.NET Core 10.0 Razor Pages application with Entity Framework Core and SQL Server (LocalDB).

## Tech Stack

- **.NET 10.0** (`net10.0` in `xiaoliran.csproj`)
- **ASP.NET Core Razor Pages** with Bootstrap 5 + jQuery + jQuery Validation (vendored in `wwwroot/lib/`)
- **Entity Framework Core 10.0.7** (SqlServer provider)
- **Serilog.AspNetCore 10.0.0** — logs to console + `logs/log-YYYYMMDD.txt` (daily rolling, 10MB max, min level Warning, Microsoft overridden to Information)
- **Database**: LocalDB (`cleandb`), Windows auth, connection string hardcoded in `Program.cs`
- **Session-based auth**: `AddSession()` + `UseSession()` — stores UserId, UserName, RealName, Gender in session after login
- **Namespace**: `xiaoliran` (used in `_ViewImports.cshtml`, all model/page references)

## Build and Run

```bash
dotnet build          # Build the project
dotnet run            # Run the dev server (http://localhost:5079 or https://localhost:7181)
dotnet watch run      # Run with hot reload
```

Dev server ports come from `Properties/launchSettings.json`: HTTP **5079**, HTTPS **7181**.

## Database

- **Auto-created at startup**: `db.Database.EnsureCreated()` is called in `Program.cs`. Only `tb_user` table is created.
- **No EF Migrations**: The project uses `EnsureCreated()`, not `EnsureMigrated()` or `dotnet ef database update`. Do not add migration files — they won't be applied automatically.
- **Connection string**: `Server=(localdb)\MSSQLLocalDB;Database=cleandb;Trusted_Connection=True;TrustServerCertificate=True;` — Windows Integrated auth (NOT `User Id=sa`), hardcoded in `Program.cs` line 33.
- **Table name**: `tb_user` (configured via `OnModelCreating` → `modelBuilder.Entity<TbUser>().ToTable("tb_user")`). Do NOT use `[Table]` attribute — it fails to resolve in this .NET 10 setup. Configure table/column mapping in `AppDbContext.OnModelCreating` instead.
- **CreateTime**: Uses `datetime` type with `GETDATE()` default (not `datetime2`).
- **Recreate database**: If table structure is wrong, drop via `sqlcmd -S "(localdb)\MSSQLLocalDB"` with `ALTER DATABASE cleandb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE cleandb;`, then `dotnet run` to recreate.

## Architecture

```
Program.cs              # Serilog config, DI (RazorPages, Session, EF Core, HttpContextAccessor),
                        # EnsureCreated(), middleware pipeline, /Personal minimal API, /api/register minimal API
appsettings.json         # DetailedErrors=true, logging defaults
appsettings.Development.json  # Logging levels, AllowedHosts=*
Data/
  AppDbContext.cs       # EF DbContext — DbSet<TbUsers>, OnModelCreating (table mapping + CreateTime config)
Models/
  TbUser.cs             # User entity (Id, Username, Password, RealName, Gender, CreateTime)
Pages/
  Shared/
    _AppLayout.cshtml      # Post-login layout: sidebar nav + top bar with avatar, profile popup
    _Layout.cshtml         # Pre-login shared layout (Bootstrap navbar/footer)
    _ValidationScriptsPartial.cshtml
  Login.cshtml/.cs        # Login — standalone (Layout=null), validates credentials, sets 4 session keys → redirects /Dashboard
  Register.cshtml/.cs     # Registration — standalone (Layout=null), POST → creates user, toast + redirects /Login
  Dashboard.cshtml/.cs    # Post-login home — stat cards + recent orders (HARDCODED mock data)
  LaundryShop.cshtml/.cs  # Post-login laundry shop table (HARDCODED mock data)
  Clothing.cshtml/.cs     # Post-login clothing table (HARDCODED mock data)
  Logout.cshtml/.cs       # POST-only: clears session, redirects to /Login
  Index.cshtml            # Pre-login default home
  Privacy.cshtml / Error.cshtml
  _ViewStart.cshtml       # Default Layout = "_Layout"
  _ViewImports.cshtml     # Imports xiaoliran, xiaoliran.Pages, TagHelpers
Sql/
  Create_tb_user.sql    # Reference SQL script (not used — EnsureCreated() handles creation)
wwwroot/
  css/
    site.css          # Default ASP.NET overrides
    login.css         # Login/register gradient theme (#667eea → #764ba2), glassmorphism card, animated circles
    app.css           # Post-login: sidebar, stat cards, data tables, badges, profile popup, toast
  js/site.js            # Empty placeholder
  lib/                  # Bootstrap 5, jQuery, jQuery Validation, jQuery Validation Unobtrusive (vendored)
logs/                   # Serilog log files (auto-created)
docs/superpowers/       # Design specs and implementation plans (specs/, plans/)
Properties/
  launchSettings.json   # HTTP 5079, HTTPS 7181
```

## Key Patterns

- **Two layouts**: Pre-login pages set `Layout = null` (Login, Register) or use `_Layout.cshtml` (Index, Privacy, Error). Post-login pages use `_AppLayout.cshtml` with sidebar and `ViewData["Title"]` for page titles.
- **Session auth**: On login success, 4 session keys are set: `UserId`, `UserName`, `RealName`, `Gender`. Pages relying on session should check `Session.GetString("UserId")` and redirect to `/Login` if null. Dashboard/LaundryShop/Clothing currently lack this check.
- **No Personal.cshtml/.cs**: The `/Personal` endpoint is NOT a Razor Page — it's a minimal API endpoint defined in `Program.cs`. It handles both GET-like read (via session in _AppLayout) and POST (AJAX form submit for updating profile). When it receives `X-Requested-With: XMLHttpRequest` header, it returns JSON `{ success, message, realName, initial, gender }`.
- **/api/register minimal API**: Accepts JSON body (bound to `RegisterRequest` record), returns JSON response. Separate from the `/Register` Razor Pages POST handler.
- **Profile popup**: `_AppLayout` top bar has an integrated profile popup. The AJAX form submit POSTs to `/Personal` and updates the avatar/display name in-place on success.
- **Toast UI pattern**: Login uses `ErrorMessage` property + Bootstrap toast (3s auto-dismiss). Register uses `Message`/`IsSuccess` properties. Register success shows toast then redirects to /Login after 3s.
- **Hardcoded mock data**: Dashboard, LaundryShop, Clothing all display static HTML tables — no DB queries yet. Dashboard shows order data with status badges (`pending`, `washing`, `done`, `delivered`); LaundryShop shows store data; Clothing shows garment data.
- **Error handling**: `LoginModel.OnPost`, `/api/register`, and `/Personal` minimal API all wrap DB calls in try-catch → friendly Chinese messages.
- **Passwords in plaintext**: No hashing implemented yet. Comparison is direct string equality.
- **Chinese UI**: All user-facing text is Chinese (zh-CN).
- **No `[Table]` attribute on models** — use `OnModelCreating` in `AppDbContext` for table/column configuration. `[Key]` attribute on `TbUser.Id` is the only data annotation used.
- **Logout is POST-only**: The `/Logout` endpoint only responds to POST requests (form submission from _AppLayout sidebar footer).
- **CSS architecture**: `site.css` = default ASP.NET overrides; `login.css` = login/register gradient theme; `app.css` = post-login layout components. All CSS files use `asp-append-version="true"`.
- **SVG icons**: All icons are inline SVGs (Feather-style), not icon font libraries.

## Important Notes

- **Git is initialized** (branch: master).
- **No appsettings connection string**: DB string is in `Program.cs` directly.
- **No `.cursor/`, `.cursorrules`, or `copilot-instructions.md`** files exist.
- **No `README.md`** exists.
- **`DateTime.Now`** is used in `TbUser` default value and `GETDATE()` in DB config — both return local (Beijing) time on the Windows server.
- **`RegisterModel.OnPost` redirects to `/Login` on success** — the success toast in Register.cshtml (`IsSuccess == true`) never renders because `RedirectToPage` is returned. The toast only renders for failure cases where `Page()` is returned.
- **`HttpContextAccessor` is registered** (`AddHttpContextAccessor()`) but not currently used in any page models — session is accessed via `HttpContext` property on `PageModel`.
