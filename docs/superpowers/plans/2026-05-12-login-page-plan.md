# Login Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a login page with hard-coded credential validation for demonstration purposes.

**Architecture:** A Razor Pages login form with POST handler that validates against hard-coded credentials. On success, redirects to Index. On failure, re-renders the page with an error message. No session management yet — that will be added during SQL Server integration.

**Tech Stack:** ASP.NET Core 10.0 Razor Pages, Bootstrap 5 (already in project)

---

### Task 1: Create Login Page

**Files:**
- Create: `Pages/Login.cshtml`
- Create: `Pages/Login.cshtml.cs`

- [ ] **Step 1: Create Login.cshtml.cs (PageModel)**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace xiaoliran.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (Username == "admin" && Password == "123456")
            {
                return RedirectToPage("/Index");
            }

            ErrorMessage = "用户名或密码错误";
            return Page();
        }
    }
}
```

- [ ] **Step 2: Create Login.cshtml (Razor view)**

```html
@page
@model xiaoliran.Pages.LoginModel
@{
    ViewData["Title"] = "Login";
}

<div class="row justify-content-center">
    <div class="col-md-6 col-lg-4">
        <div class="card mt-5">
            <div class="card-header text-center">
                <h4>登录</h4>
            </div>
            <div class="card-body">
                <form method="post">
                    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

                    @if (!string.IsNullOrEmpty(Model.ErrorMessage))
                    {
                        <div class="alert alert-danger" role="alert">
                            @Model.ErrorMessage
                        </div>
                    }

                    <div class="mb-3">
                        <label asp-for="Username" class="form-label"></label>
                        <input asp-for="Username" class="form-control" placeholder="请输入用户名" />
                        <span asp-validation-for="Username" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Password" class="form-label"></label>
                        <input asp-for="Password" type="password" class="form-control" placeholder="请输入密码" />
                        <span asp-validation-for="Password" class="text-danger"></span>
                    </div>

                    <button type="submit" class="btn btn-primary w-100">登录</button>
                </form>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

- [ ] **Step 3: Verify it runs**

Run: `dotnet run`
Expected: Application starts without errors, navigate to `https://localhost:<port>/Login` to see the login page.

- [ ] **Step 4: Commit**

```bash
git add Pages/Login.cshtml Pages/Login.cshtml.cs
git commit -m "feat: add login page with hard-coded credential validation"
```
