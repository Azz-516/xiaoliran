# Login Page Design

## Scope
Add a login page to the xiaoliran Razor Pages app. Hard-coded credentials for now; SQL Server integration planned for later.

## Components
- `Pages/Login.cshtml` — Centered login card with username, password fields and login button. Uses Bootstrap form styling.
- `Pages/Login.cshtml.cs` — PageModel with `OnPostAsync`. Hard-coded `admin` / `123456` validation. Redirect to `/Index` on success, re-render with error on failure.

## Defaults
- Default routes unchanged. Index remains the home page. Login accessed via `/Login`.
