using Microsoft.EntityFrameworkCore;
using xiaoliran.Models;

namespace xiaoliran.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<TbUser> TbUsers => Set<TbUser>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<LaundryShop> LaundryShops => Set<LaundryShop>();
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Existing TbUser config
            modelBuilder.Entity<TbUser>().ToTable("tb_user");
            modelBuilder.Entity<TbUser>()
                .Property(u => u.CreateTime)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");

            // Role config
            modelBuilder.Entity<Role>().ToTable("tb_role");
            modelBuilder.Entity<Role>()
                .Property(r => r.RoleKey)
                .HasMaxLength(50)
                .IsRequired();
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleKey)
                .IsUnique();
            modelBuilder.Entity<Role>()
                .Property(r => r.CreateTime)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");

            // UserRole config
            modelBuilder.Entity<UserRole>().ToTable("tb_user_role");
            modelBuilder.Entity<UserRole>()
                .Property(ur => ur.CreateTime)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");

            // Permission config
            modelBuilder.Entity<Permission>().ToTable("tb_permission");
            modelBuilder.Entity<Permission>()
                .Property(p => p.PermissionKey)
                .HasMaxLength(100)
                .IsRequired();
            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.PermissionKey)
                .IsUnique();
            modelBuilder.Entity<Permission>()
                .Property(p => p.CreateTime)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");

            // RolePermission config
            modelBuilder.Entity<RolePermission>().ToTable("tb_role_permission");
            modelBuilder.Entity<RolePermission>()
                .Property(rp => rp.CreateTime)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");

            // LaundryShop config
            modelBuilder.Entity<LaundryShop>().ToTable("tb_laundry_shop");
            modelBuilder.Entity<LaundryShop>()
                .Property(s => s.Status)
                .HasMaxLength(10)
                .IsRequired();
            modelBuilder.Entity<LaundryShop>()
                .Property(s => s.CreateTime)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");

            // Order config
            modelBuilder.Entity<Order>().ToTable("tb_order");
            modelBuilder.Entity<Order>()
                .Property(o => o.OrderNo)
                .HasMaxLength(50)
                .IsRequired();
            modelBuilder.Entity<Order>()
                .Property(o => o.ServiceType)
                .HasMaxLength(20)
                .IsRequired();
            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasMaxLength(20)
                .IsRequired();
            modelBuilder.Entity<Order>()
                .Property(o => o.EstimatedCost)
                .HasPrecision(10, 2);
            modelBuilder.Entity<Order>()
                .Property(o => o.CreateTime)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");
        }
    }
}
