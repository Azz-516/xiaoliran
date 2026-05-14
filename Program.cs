using xiaoliran.Data;
using Microsoft.EntityFrameworkCore;
using xiaoliran.Models;
using Serilog;

namespace xiaoliran
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Starting up");

                var builder = WebApplication.CreateBuilder(args);
                builder.Host.UseSerilog();

                builder.Services.AddRazorPages();
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddSession();

                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=cleandb;Trusted_Connection=True;TrustServerCertificate=True;"));

                var app = builder.Build();

                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.EnsureCreated();
                    SeedInitialData(db);
                    SeedTestData(db);
                }

                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();
                app.UseSession();
                app.UseAuthorization();

                app.MapStaticAssets();
                app.MapRazorPages().WithStaticAssets();

                app.MapPost("/Personal", async (HttpContext httpContext, AppDbContext db) =>
                {
                    try
                    {
                        var userId = httpContext.Session.GetString("UserId");
                        if (string.IsNullOrEmpty(userId))
                        {
                            return Results.Json(new { success = false, message = "未登录" });
                        }

                        var form = await httpContext.Request.ReadFormAsync();
                        var realName = form["RealName"].ToString();
                        var gender = form["Gender"].ToString();
                        var phone = form["Phone"].ToString();
                        var password = form["Password"].ToString();

                        var user = await db.TbUsers.FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
                        if (user == null)
                        {
                            return Results.Json(new { success = false, message = "用户不存在" });
                        }

                        user.RealName = realName;
                        user.Gender = gender;
                        if (!string.IsNullOrEmpty(phone) && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^1[3-9]\d{9}$"))
                        {
                            return Results.Json(new { success = false, message = "请输入合法的手机号码" });
                        }
                        user.Phone = phone;
                        if (!string.IsNullOrWhiteSpace(password))
                        {
                            user.Password = password;
                        }

                        await db.SaveChangesAsync();

                        httpContext.Session.SetString("RealName", realName);
                        httpContext.Session.SetString("Gender", gender);
                        httpContext.Session.SetString("Phone", phone);

                        var initial = realName.Length > 0 ? realName.Substring(0, 1) : "?";

                        return Results.Json(new
                        {
                            success = true,
                            message = "保存成功",
                            realName = realName,
                            initial = initial,
                            gender = gender
                        });
                    }
                    catch (Exception)
                    {
                        return Results.Json(new { success = false, message = "服务异常，请稍后重试" });
                    }
                });

                app.MapPost("/api/register", async (RegisterRequest request, AppDbContext db) =>
                {
                    try
                    {
                        if (await db.TbUsers.AnyAsync(u => u.Username == request.Username))
                        {
                            return Results.BadRequest(new { success = false, message = "用户名已存在" });
                        }

                        var user = new TbUser
                        {
                            Username = request.Username,
                            Password = request.Password,
                            RealName = request.RealName,
                            Gender = request.Gender,
                            Phone = request.Phone ?? ""
                        };

                        db.TbUsers.Add(user);
                        await db.SaveChangesAsync();

                        // Assign default 'user' role
                        var userRoleId = await db.Roles
                            .Where(r => r.RoleKey == "user")
                            .Select(r => r.Id)
                            .FirstOrDefaultAsync();

                        if (userRoleId > 0)
                        {
                            db.UserRoles.Add(new UserRole
                            {
                                UserId = user.Id,
                                RoleId = userRoleId
                            });
                            await db.SaveChangesAsync();
                        }

                        return Results.Ok(new { success = true, message = "注册成功" });
                    }
                    catch (Exception)
                    {
                        return Results.BadRequest(new { success = false, message = "服务异常，请稍后重试" });
                    }
                });

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

                void SeedTestData(AppDbContext db)
                {
                    if (db.TbUsers.Any(u => u.Username.StartsWith("test_user_"))) return;

                    var names = new[] { "王小明", "李小红", "张伟", "刘洋", "陈静", "杨帆", "赵敏", "孙涛", "周莉", "吴刚", "郑洁", "王磊", "黄丽", "林峰", "何秀英", "马超", "罗文", "梁芳", "宋杰", "谢娜", "韩梅", "唐亮", "冯雪", "曹鹏", "邓婷", "萧剑", "田甜", "潘石", "蒋欣", "蔡依" };
                    var users = new List<TbUser>();
                    for (int i = 1; i <= 50; i++)
                    {
                        var n = names[(i - 1) % names.Length];
                        users.Add(new TbUser
                        {
                            Username = $"test_user_{i:D3}",
                            Password = "123456",
                            RealName = n + i,
                            Gender = i % 2 == 0 ? "男" : "女",
                            Phone = $"138{((i * 1234567).ToString().PadLeft(8, '0').Substring(0, 8))}",
                            CreateTime = DateTime.Now.AddDays(-i)
                        });
                    }
                    db.TbUsers.AddRange(users);
                    db.SaveChanges();

                    var areas = new[] { "东校区", "西校区", "南校区", "北校区" };
                    var prefixes = new[] { "清爽", "洁净", "亮洁", "净美", "舒心", "快洁", "优洗", "干净点", "洗衣邦", "衣洁净" };
                    var shops = new List<LaundryShop>();
                    for (int i = 1; i <= 50; i++)
                    {
                        var area = areas[(i - 1) % 4];
                        var pfx = prefixes[(i - 1) % prefixes.Length];
                        shops.Add(new LaundryShop
                        {
                            Name = $"{pfx}洗衣店{area}{i}号",
                            Address = $"{area}生活区{('A' + (i % 8)):c}栋{(i % 5 + 1)}层",
                            ContactPhone = $"138{(i * 123456).ToString().PadLeft(8, '0').Substring(0, 8)}",
                            ContactPerson = $"负责人{i}",
                            Status = i % 10 == 0 ? "已停业" : "营业中",
                            BusinessHours = "08:00-20:00",
                            Description = $"这是第{i}家门店，提供干洗、水洗、熨烫等服务。"
                        });
                    }
                    db.LaundryShops.AddRange(shops);
                    db.SaveChanges();

                    Console.WriteLine($"Seed completed: {db.TbUsers.Count()} users, {db.LaundryShops.Count()} shops");
                }

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }

    public record RegisterRequest(string Username, string Password, string RealName, string Gender, string Phone);
}
