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
- **Session-based auth**: `AddSession()` + `UseSession()` — stores UserId, UserName, RealName in session after login
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
Data/
  AppDbContext.cs       # EF DbContext — DbSet<TbUser>, OnModelCreating (table mapping + CreateTime config)
Models/
  TbUser.cs             # User entity (Id, Username, Password, RealName, Gender, CreateTime)
Pages/
  Shared/
    _AppLayout.cshtml      # Post-login layout: sidebar nav + top bar with avatar circle (first char of RealName)
    _Layout.cshtml         # Pre-login shared layout (Bootstrap navbar/footer)
    _ValidationScriptsPartial.cshtml
  Login.cshtml/.cs        # Login — standalone, validates credentials, sets session → redirects to /Dashboard
  Register.cshtml/.cs     # Registration — standalone, POST handler, toast messages (3s auto-dismiss)
  Dashboard.cshtml/.cs    # Post-login home — stat cards + recent orders table (HARDCODED mock data)
  LaundryShop.cshtml/.cs  # Post-login laundry shop management table (HARDCODED mock data)
  Clothing.cshtml/.cs     # Post-login clothing management table (HARDCODED mock data)
  Personal.cshtml/.cs     # Post-login personal info — reads/writes tb_user, modifies session on name change
  Logout.cshtml/.cs       # Clears session, redirects to /Login
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
- **Session auth**: On login success, `HttpContext.Session.SetString("UserId"/"UserName"/"RealName")`. Personal page reads session for user ID. No auth middleware — pages relying on session should check `Session.GetString("UserId")` and redirect to `/Login` if null.
- **Toast error UI**: Both Login and Register show Bootstrap toast notifications (3s auto-dismiss) for all errors including DB failures.
- **Hardcoded mock data**: Dashboard, LaundryShop, Clothing all display static HTML tables — no DB queries yet. Dashboard shows order data; LaundryShop shows store data; Clothing shows garment data.
- **Error handling**: Both `LoginModel.OnPost` and `/api/register` wrap DB calls in try-catch → friendly messages.
- **Passwords in plaintext**: No hashing implemented yet.
- **Chinese UI**: All user-facing text is Chinese (zh-CN).
- **No `[Table]` attribute on models** — use `OnModelCreating` in `AppDbContext` for table/column configuration.

## Important Notes

- **No git repository**: Not under version control.
- **No appsettings connection string**: DB string is in `Program.cs` directly.
- **No `.cursor/`, `.cursorrules`, or `copilot-instructions.md`** files exist.
- **No `README.md`** exists.
