using FeedFlow.Domain;
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

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<Product>().HasIndex(x => new { x.OrgId, x.Sku }).IsUnique();
            b.Entity<Feed>().HasIndex(x => new { x.OrgId, x.Channel });
        }
    }
}
