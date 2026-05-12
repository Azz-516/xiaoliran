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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TbUser>().ToTable("tb_user");
            modelBuilder.Entity<TbUser>()
                .Property(u => u.CreateTime)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");
        }
    }
}
