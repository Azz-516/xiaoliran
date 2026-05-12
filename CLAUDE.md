# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**校园干洗店管理系统** (Campus Dry Cleaning Management System) — an ASP.NET Core 10.0 Razor Pages application with Entity Framework Core and SQL Server (LocalDB).

## Tech Stack

- **.NET 10.0** (`xiaoliran.csproj`)
- **ASP.NET Core Razor Pages** with Bootstrap 5 + jQuery + jQuery Validation
- **Entity Framework Core 10.0.7** (SqlServer provider)
- **Serilog.AspNetCore 10.0.0** — logs to console + `logs/log-YYYYMMDD.txt` (daily rolling, 10MB max)
- **Database**: LocalDB (`cleandb`), Windows auth, connection string hardcoded in `Program.cs`
- **Session-based auth**: `AddSession()` + `UseSession()` — stores UserId, UserName, RealName, Gender in session after login
- **No auth middleware**: Pages don't auto-redirect unauthenticated users. Dashboard/LaundryShop/Clothing have no session check — only Personal.cshtml redirects to /Login if session is missing.
- Only one DB table exists: `tb_user` — Dashboard/LaundryShop/Clothing pages show hardcoded mock data, no business models yet

## Build and Run

```bash
dotnet build          # Build the project
dotnet run            # Run the dev server (http://localhost:5079)
dotnet watch run      # Run with hot reload
```

## Database

- **Auto-created at startup**: `db.Database.EnsureCreated()` is called in `Program.cs` before the app starts. Only `tb_user` table is created.
- **Connection string**: `Server=(localdb)\MSSQLLocalDB;Database=cleandb;Trusted_Connection=True;TrustServerCertificate=True;` — Windows Integrated auth (NOT `User Id=sa`).
- **Table name**: `tb_user` (configured via `OnModelCreating` → `modelBuilder.Entity<TbUser>().ToTable("tb_user")`). Do NOT use `[Table]` attribute — it fails to resolve in this .NET 10 setup. Configure table/column mapping in `AppDbContext.OnModelCreating` instead.
- **CreateTime**: Uses `datetime` type with `GETDATE()` default (not `datetime2`) to match Beijing time.
- **Recreate database**: If table structure is wrong, drop via `sqlcmd -S "(localdb)\MSSQLLocalDB"` with `ALTER DATABASE cleandb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE cleandb;`, then `dotnet run` to recreate.

## Architecture

```
Program.cs              # Serilog, Session, EF Core config, middleware, /api/register minimal API
appsettings.json         # DetailedErrors=true, logging defaults
Data/
  AppDbContext.cs       # EF DbContext — DbSet<TbUser>, OnModelCreating (table mapping + CreateTime config)
Models/
  TbUser.cs             # User entity (Id, Username, Password, RealName, Gender, CreateTime)
Pages/
  Shared/
    _AppLayout.cshtml      # Post-login layout: sidebar nav + top bar with avatar (first char of RealName), integrated profile popup
    _Layout.cshtml         # Pre-login shared layout (Bootstrap navbar/footer)
    _ValidationScriptsPartial.cshtml
  Login.cshtml/.cs        # Login — standalone, validates credentials, sets 4 session keys → redirects to /Dashboard
  Register.cshtml/.cs     # Registration — standalone, POST handler, toast messages (3s auto-dismiss, then redirects to /Login on success)
  Dashboard.cshtml/.cs    # Post-login home — stat cards + recent orders table (HARDCODED mock data), no session check
  LaundryShop.cshtml/.cs  # Post-login laundry shop management table (HARDCODED mock data), no session check
  Clothing.cshtml/.cs     # Post-login clothing management table (HARDCODED mock data), no session check
  Personal.cshtml/.cs     # Post-login personal info — reads/writes tb_user, modifies session; supports AJAX POST (X-Requested-With header → JSON response)
  Logout.cshtml/.cs       # POST-only: clears session, redirects to /Login
  Index.cshtml            # Default home (pre-login placeholder)
  Privacy.cshtml / Error.cshtml
  _ViewStart.cshtml       # Default Layout = "_Layout"
  _ViewImports.cshtml     # Imports TagHelpers
Sql/
  Create_tb_user.sql    # Reference SQL script (not used — EnsureCreated() handles creation)
wwwroot/
  css/site.css          # ASP.NET Core default
  css/login.css         # Login/register gradient theme (#667eea → #764ba2)
  css/app.css           # Post-login app layout: sidebar, stat cards, data tables, badges, avatar
  js/site.js            # Empty placeholder
  lib/                  # Bootstrap 5, jQuery, jQuery Validation (vendored)
logs/                   # Serilog log files (auto-created)
docs/superpowers/       # Design specs and implementation plans
```

## Key Patterns

- **Two layouts**: Pre-login pages use `_Layout.cshtml` (or none), post-login pages use `_AppLayout.cshtml` with sidebar. Both login and register set `Layout = null` for standalone rendering.
- **Session auth**: On login success, `HttpContext.Session.SetString("UserId"/"UserName"/"RealName"/"Gender")` — 4 keys are set. Personal page reads session for user ID. No auth middleware — pages relying on session should check `Session.GetString("UserId")` and redirect to `/Login` if null. Dashboard/LaundryShop/Clothing currently lack this check.
- **Profile popup**: `_AppLayout` top bar includes an integrated profile popup that AJAX-POSTs to `/Personal`. When `/Personal` receives a request with `X-Requested-With: XMLHttpRequest` header, it returns JSON `{ success, message, realName, initial, gender }` instead of rendering a page.
- **Toast error UI**: Both Login and Register show Bootstrap toast notifications (3s auto-dismiss) for all errors including DB failures. Register redirects to /Login on success after toast dismisses.
- **Hardcoded mock data**: Dashboard, LaundryShop, Clothing all display static HTML tables — no DB queries yet. Dashboard shows order data; LaundryShop shows store data; Clothing shows garment data.
- **Error handling**: Both `LoginModel.OnPost` and `/api/register` wrap DB calls in try-catch → friendly messages.
- **Passwords in plaintext**: No hashing implemented yet.
- **Chinese UI**: All user-facing text is Chinese (zh-CN).
- **No `[Table]` attribute on models** — use `OnModelCreating` in `AppDbContext` for table/column configuration.
- **Logout is POST-only**: The `/Logout` endpoint only responds to POST requests.
- **CSS architecture**: `site.css` = default ASP.NET overrides; `login.css` = login/register gradient theme with glassmorphism card and animated circles; `app.css` = post-login sidebar layout, stat cards, data tables, badges, profile popup, toast animations.

## Important Notes

- **No git repository**: Not under version control.
- **No appsettings connection string**: DB string is in `Program.cs` directly. `appsettings.json` only contains `DetailedErrors=true` and logging defaults.
- **Dev server ports**: HTTP 5079, HTTPS 7181 (from `Properties/launchSettings.json`).
- **No `.cursor/`, `.cursorrules`, or `copilot-instructions.md`** files exist.
- **No `README.md`** exists.
- **`[Key]` attribute is used** on `TbUser.Id` — this is the only data annotation that works reliably; avoid `[Table]`.
- **Registration has two paths**: Razor Pages at `/Register` (POST → `RegisterModel.OnPost`) and minimal API at `/api/register` (POST → JSON response). Both create `TbUser` records.
- **`DateTime.Now`** is used in `TbUser` default value and `GETDATE()` in DB config — both return local (Beijing) time on the Windows server.
