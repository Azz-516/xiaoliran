# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**校园干洗店管理系统** (Campus Dry Cleaning Management System) — an ASP.NET Core 10.0 Razor Pages application with Entity Framework Core and SQL Server (LocalDB).

## Tech Stack

- **.NET 10.0** (SDK-based web project: `xiaoliran.csproj`)
- **ASP.NET Core Razor Pages** with Bootstrap 5 + jQuery
- **Entity Framework Core 10.0.7** with SQL Server provider
- **Database**: LocalDB (`cleandb`), connection string hardcoded in `Program.cs`
- No authentication/session management yet — login only validates credentials

## Build and Run

```bash
dotnet build          # Build the project
dotnet run            # Run the dev server
dotnet watch run      # Run with hot reload
```

## EF Core Migrations

No migrations folder exists yet — the project uses the SQL script `Sql/Create_tb_user.sql` to create tables manually. If you start using migrations:

```bash
dotnet ef migrations add <name>
dotnet ef database update
```

The connection string is: `Server=(localdb)\MSSQLLocalDB;Database=cleandb;User Id=sa;TrustServerCertificate=True;`

## Architecture

```
Program.cs              # App startup, DI, middleware pipeline, /api/register endpoint
Data/
  AppDbContext.cs       # EF DbContext with DbSet<TbUser>
Models/
  TbUser.cs             # User entity (Id, Username, Password, RealName, Gender, CreateTime)
Pages/
  _Layout.cshtml        # Shared layout (currently unused by login/register — they set Layout = null)
  _ViewStart.cshtml     # Default layout = "_Layout"
  _ViewImports.cshtml   # Tag helpers import
  Index.cshtml          # Home page (default route)
  Login.cshtml/.cs      # Login page — validates against TbUsers table
  Register.cshtml/.cs   # Registration page — POST form handler + /api/register minimal API
  Privacy.cshtml
  Error.cshtml
  Shared/
    _ValidationScriptsPartial.cshtml
Sql/
  Create_tb_user.sql    # Manual SQL script for creating tb_user table
wwwroot/
  css/site.css          # Global styles
  css/login.css         # Login/register page specific styles (gradient background, card UI)
  js/site.js            # Empty JS placeholder
  lib/                  # Bootstrap 5, jQuery, jQuery Validation (vendored)
docs/superpowers/       # Design specs and implementation plans
```

## Key Patterns

- **PageModel + Razor Pages**: Login and Register use `PageModel` with `[BindProperty]` and `OnPost()` handlers. Both pages set `Layout = null` and render standalone forms.
- **Minimal API endpoint**: `Program.cs` also defines a `/api/register` POST endpoint as an alternative to the Razor Pages registration flow.
- **Passwords stored in plaintext**: No hashing/encryption is implemented yet.
- **Chinese UI**: All user-facing text is in Chinese (zh-CN).
- **Design theme**: Login/Register pages use a purple-blue gradient (`#667eea` → `#764ba2`) with floating circle decorations and card-based layout defined in `wwwroot/css/login.css`.

## Important Notes

- **No git repository**: This project is not under version control.
- **No appsettings connection string**: The DB connection string is hardcoded directly in `Program.cs`, not in `appsettings.json`.
- **`_ViewStart.cshtml` sets default layout to `_Layout`**, but Login and Register explicitly override with `Layout = null` since they have their own full-page designs.
- **No `.cursor/`, `.cursorrules`, or `copilot-instructions.md`** files exist.
- **No `README.md`** exists.
