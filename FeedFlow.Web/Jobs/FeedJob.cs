using FeedFlow.Web.Data;
using FeedFlow.Infrastructure.Feeds;
using Microsoft.EntityFrameworkCore;

public class FeedJob
{
    private readonly AppDbContext _db;
    private readonly GoogleMerchantFeedBuilder _builder;

    public FeedJob(AppDbContext db, GoogleMerchantFeedBuilder builder) { _db = db; _builder = builder; }

    public async Task BuildGoogleFeed(Guid orgId)
    {
        var org = await _db.Orgs.FirstAsync(o => o.Id == orgId);
        var prods = await _db.Products.Where(p => p.OrgId == orgId && p.IsActive).ToListAsync();
        var settings = await _db.StoreSettings.FirstOrDefaultAsync(s => s.OrgId == orgId);
        var storeName = settings?.StoreName ?? org.Name;
        var url = await _builder.BuildAsync(org, prods, storeName);
        var feed = await _db.Feeds.FirstOrDefaultAsync(f => f.OrgId == orgId && f.Channel == "google-merchant")
                   ?? _db.Feeds.Add(new FeedFlow.Domain.Feed { OrgId = orgId, Name = "Google Merchant" }).Entity;
        feed.PublicUrl = url;
        feed.LastBuiltAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }
}
