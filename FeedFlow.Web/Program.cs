using System;
using System.IO;
using System.Linq;
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

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

var feedsPhysicalPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "feeds");
Directory.CreateDirectory(feedsPhysicalPath);

var useAzureStorage = string.Equals(builder.Configuration["UseAzureStorage"], "true", StringComparison.OrdinalIgnoreCase);
if (useAzureStorage)
{
    var azureConn = builder.Configuration["AzureStorage:ConnectionString"];
    var azureContainer = builder.Configuration["AzureStorage:Container"];
    if (string.IsNullOrWhiteSpace(azureConn) || string.IsNullOrWhiteSpace(azureContainer))
        throw new InvalidOperationException("UseAzureStorage=true but AzureStorage settings are missing.");
    builder.Services.AddSingleton<IFileStorage>(new AzureBlobStorage(azureConn!, azureContainer!));
}
else
{
    builder.Services.AddSingleton<IFileStorage>(new LocalFileStorage(feedsPhysicalPath));
}

var useSqlite = string.Equals(builder.Configuration["UseSqlite"], "true", StringComparison.OrdinalIgnoreCase);
if (useSqlite)
{
    var sqliteConn = builder.Configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(sqliteConn))
    {
        var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        sqliteConn = $"Data Source={Path.Combine(dataDir, "feedflow.db")}";
    }
    else
    {
        const string key = "Data Source=";
        var i = sqliteConn.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i >= 0)
        {
            var path = sqliteConn[(i + key.Length)..].Trim().Trim('"');
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }
    }

    Console.WriteLine($"[DB] SQLite: {sqliteConn}");
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(sqliteConn));
    builder.Services.AddHangfire(cfg => cfg.UseMemoryStorage());
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
    builder.Services.AddHangfire(cfg => cfg.UseSqlServerStorage(builder.Configuration.GetConnectionString("Default")));
}

builder.Services.AddDefaultIdentity<ApplicationUser>(o => o.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();

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

static async Task<bool> EnsureDbAndSeedAsync(WebApplication app, bool usingSqlite)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (usingSqlite)
        {
            Console.WriteLine("[DB] Using SQLite -> EnsureCreated");
            await db.Database.EnsureCreatedAsync();
            Console.WriteLine("[DB] EnsureCreated complete.");
        }
        else
        {
            Console.WriteLine("[DB] Using SQL Server -> Migrate");
            await db.Database.MigrateAsync();
            Console.WriteLine("[DB] Migrations complete.");
        }

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
            var created = await userMgr.CreateAsync(user, "Passw0rd!");
            Console.WriteLine("[DB] Seed admin: " + (created.Succeeded ? "OK" : "FAILED"));
        }

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine("[DB] EnsureDbAndSeed failed: " + ex.Message);
        return false;
    }
}

var usingSqlite = useSqlite;
var dbReady = await EnsureDbAndSeedAsync(app, usingSqlite);

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

app.Run();
