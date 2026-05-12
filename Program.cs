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
                            Gender = request.Gender
                        };

                        db.TbUsers.Add(user);
                        await db.SaveChangesAsync();

                        return Results.Ok(new { success = true, message = "注册成功" });
                    }
                    catch (Exception)
                    {
                        return Results.BadRequest(new { success = false, message = "服务异常，请稍后重试" });
                    }
                });

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

    public record RegisterRequest(string Username, string Password, string RealName, string Gender);
}
