using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace xiaoliran.Models
{
    public static class PermissionHelper
    {
        public static bool CheckPermission(PageModel page, string permissionKey)
        {
            var session = page.HttpContext.Session;
            var userId = session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                page.HttpContext.Response.Redirect("/Login");
                return false;
            }

            var permissions = session.GetString("UserPermissions") ?? "";
            if (!permissions.Contains(permissionKey))
            {
                page.HttpContext.Response.Redirect("/Dashboard?toast=无权限访问此页面");
                return false;
            }

            return true;
        }
    }
}
