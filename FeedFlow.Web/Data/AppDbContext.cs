using FeedFlow.Domain;
using FeedFlow.Domain.Entities;
using FeedFlow.Web.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FeedFlow.Web.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Org> Orgs => Set<Org>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Feed> Feeds => Set<Feed>();
        public DbSet<BuildRun> BuildRuns => Set<BuildRun>();
        public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<Product>().HasIndex(x => new { x.OrgId, x.Sku }).IsUnique();
            b.Entity<Feed>().HasIndex(x => new { x.OrgId, x.Channel });
            b.Entity<StoreSettings>().HasIndex(s => s.OrgId).IsUnique();
            b.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            b.Entity<Product>().Property(p => p.SalePrice).HasPrecision(18, 2);
        }
    }
}
