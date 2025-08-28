using Hangfire.Dashboard;

public class HangfireDashboardAuth : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
        => context.GetHttpContext().User?.Identity?.IsAuthenticated == true;
}
