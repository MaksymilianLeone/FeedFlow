using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace FeedFlow.Web.Identity
{
    public sealed class HangfireDashboardAuth : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var http = context.GetHttpContext();

            var result = http.AuthenticateAsync(IdentityConstants.ApplicationScheme)
                             .GetAwaiter().GetResult();

            return result.Succeeded && http.User?.Identity?.IsAuthenticated == true;
        }
    }
}
