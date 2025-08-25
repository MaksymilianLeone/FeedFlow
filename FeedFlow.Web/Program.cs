using FeedFlow.Web.Data;
using FeedFlow.Web.Identity;
using FeedFlow.Infrastructure.Storage;            
using FeedFlow.Infrastructure.Feeds;            
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddDefaultIdentity<ApplicationUser>(o => o.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Storage (local)
builder.Services.AddSingleton<IFileStorage>(sp =>
    new LocalFileStorage(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "feeds")));
builder.Services.AddScoped<GoogleMerchantFeedBuilder>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();

// serve /feeds from wwwroot/feeds
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine (app.Environment.ContentRootPath, "wwwroot", "feeds")),
    RequestPath = "/feeds"
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
