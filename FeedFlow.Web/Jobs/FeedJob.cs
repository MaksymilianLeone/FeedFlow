using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FeedFlow.Domain;
using FeedFlow.Infrastructure.Feeds;
using FeedFlow.Web.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FeedFlow.Web.Jobs
{
    public class FeedJob
    {
        private readonly AppDbContext _db;
        private readonly GoogleMerchantFeedBuilder _builder;
        private readonly ILogger<FeedJob> _log;

        public FeedJob(AppDbContext db, GoogleMerchantFeedBuilder builder, ILogger<FeedJob> log)
        {
            _db = db;
            _builder = builder;
            _log = log;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 1800)]
        public Task BuildGoogleFeed(Guid orgId) => BuildGoogleFeed_Internal(orgId, null, CancellationToken.None);

        [DisableConcurrentExecution(timeoutInSeconds: 1800)]
        public Task BuildGoogleFeed(Guid orgId, Guid runId) => BuildGoogleFeed_Internal(orgId, runId, CancellationToken.None);

        public Task BuildGoogleFeed(Guid orgId, CancellationToken ct) => BuildGoogleFeed_Internal(orgId, null, ct);

        private async Task BuildGoogleFeed_Internal(Guid orgId, Guid? existingRunId, CancellationToken ct)
        {
            var org = await _db.Orgs.FirstAsync(o => o.Id == orgId, ct);
            var products = await _db.Products.Where(p => p.OrgId == orgId && p.IsActive).ToListAsync(ct);
            var settings = await _db.StoreSettings.FirstOrDefaultAsync(s => s.OrgId == orgId, ct);
            var storeName = settings?.StoreName ?? org.Name;

            var feed = await _db.Feeds.FirstOrDefaultAsync(f => f.OrgId == orgId && f.Channel == "google-merchant", ct)
                       ?? _db.Feeds.Add(new Feed { OrgId = orgId, Channel = "google-merchant", Name = "Google Merchant" }).Entity;
            await _db.SaveChangesAsync(ct);

            BuildRun run;
            if (existingRunId.HasValue)
            {
                run = await _db.BuildRuns.FirstAsync(r => r.Id == existingRunId.Value, ct);
                run.Status = "Running";
            }
            else
            {
                run = new BuildRun { FeedId = feed.Id, Status = "Running", StartedAt = DateTimeOffset.UtcNow };
                _db.BuildRuns.Add(run);
            }
            await _db.SaveChangesAsync(ct);

            try
            {
                var url = await _builder.BuildAsync(
                          org, products, storeName,
                          settings?.UtmSource, settings?.UtmMedium, settings?.UtmCampaign,
                          settings?.BaseUrl, ct);
                feed.PublicUrl = url;
                feed.LastBuiltAt = DateTimeOffset.UtcNow;
                run.Status = "Succeeded";
                run.EndedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(ct);
                _log.LogInformation("Built Google feed for Org {OrgId}: {Url}", orgId, url);
            }
            catch (Exception ex)
            {
                run.Status = "Failed";
                run.EndedAt = DateTimeOffset.UtcNow;
                run.ErrorsJson = ex.ToString();
                await _db.SaveChangesAsync(ct);
                _log.LogError(ex, "Failed building Google feed for Org {OrgId}", orgId);
                throw;
            }
        }
    }
}
