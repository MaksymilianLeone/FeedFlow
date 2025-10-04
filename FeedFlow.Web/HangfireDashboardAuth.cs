using Hangfire.Dashboard;

namespace FeedFlow.Web.Identity
{
    public sealed class HangfireDashboardAuth : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var http = context.GetHttpContext();
            return http.User?.Identity?.IsAuthenticated == true;
        }
    }
}
