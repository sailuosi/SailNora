using Hangfire.Dashboard;

namespace SailNora.App.Scheduling;

public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true; // 开发期放行；生产需收紧
}
