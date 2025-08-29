using FeedFlow.Infrastructure.Feeds;
using FeedFlow.Infrastructure.Storage;
using FeedFlow.Web.Data;
using FeedFlow.Web.Identity;
using FeedFlow.Web.Jobs;
using Hangfire;
using Hangfire.MemoryStorage; 
using Hangfire.SqlServer;     
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);
var feedsPhysicalPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "feeds");
Directory.CreateDirectory(feedsPhysicalPath);

var azureConn = builder.Configuration["AzureStorage:ConnectionString"];
var azureContainer = builder.Configuration["AzureStorage:Container"];
var useAzure =
    builder.Environment.IsProduction() &&
    !string.IsNullOrWhiteSpace(azureConn) &&
    !string.IsNullOrWhiteSpace(azureContainer);

if (useAzure)
{
    builder.Services.AddSingleton<IFileStorage>(new AzureBlobStorage(azureConn!, azureContainer!));
}
else
{
    builder.Services.AddSingleton<IFileStorage>(new LocalFileStorage(feedsPhysicalPath));
}

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddDefaultIdentity<ApplicationUser>(o => o.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHangfire(cfg => cfg.UseMemoryStorage());
}
else
{
    builder.Services.AddHangfire(cfg =>
        cfg.UseSqlServerStorage(builder.Configuration.GetConnectionString("Default")));
}
builder.Services.AddHangfireServer();

builder.Services.AddScoped<GoogleMerchantFeedBuilder>();
builder.Services.AddScoped<FeedJob>();

var app = builder.Build();

app.MapRazorPages();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}



app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(feedsPhysicalPath),
    RequestPath = "/feeds"
});

app.UseRouting();
app.UseAuthentication();  
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuth() }
});
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
static async Task<bool> EnsureDbAndSeedAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var org = await db.Orgs.FirstOrDefaultAsync();
        if (org is null)
        {
            org = new FeedFlow.Domain.Org { Name = "Demo Store" };
            await db.Orgs.AddAsync(org);
            await db.SaveChangesAsync();
        }

        var admin = await userMgr.FindByNameAsync("admin@local");
        if (admin is null)
        {
            var user = new ApplicationUser
            {
                UserName = "admin@local",
                Email = "admin@local",
                OrgId = org.Id,
                EmailConfirmed = true
            };
            await userMgr.CreateAsync(user, "Passw0rd!");
        }

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Seed] Skipping seed due to DB error: {ex.Message}");
        return false;
    }
}

var dbReady = await EnsureDbAndSeedAsync(app);

if (dbReady)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    foreach (var orgId in db.Orgs.Select(o => o.Id).ToList())
    {
        RecurringJob.AddOrUpdate<FeedJob>(
            $"org-{orgId}-google",
            j => j.BuildGoogleFeed(orgId),
            Cron.Daily(3)); 
    }
}
BackgroundJob.Enqueue(() =>
    Console.WriteLine($"[HF TEST] Hangfire test job ran at {DateTimeOffset.UtcNow:u}"));
app.Run();
