using Hangfire;
using Hangfire.Dashboard;
using mooSQL.data;
using SailNora.Foundation.Data;
using SailNora.Foundation.Options;

namespace SailNora.App.Scheduling;

public static class HangfireSetupExtensions
{
    public static IServiceCollection AddSailNoraHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(HangfireHostOptions.SectionName).Get<HangfireHostOptions>()
                      ?? new HangfireHostOptions();
        if (!options.Enabled)
            return services;

        services.AddHangfire((sp, config) =>
        {
            var cash = sp.GetRequiredService<DBInsCash>();
            var db = cash.getInstance(MooSqlSetup.DefaultPosition);
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMooSqlStorage(db, opt =>
                {
                    opt.TablesPrefix = options.TablesPrefix;
                    opt.PrepareSchemaIfNecessary = true;
                });
        });

        services.AddHangfireServer();
        services.AddScoped<RefreshTokenCleanupJob>();
        return services;
    }

    public static WebApplication UseSailNoraHangfire(this WebApplication app)
    {
        var options = app.Configuration.GetSection(HangfireHostOptions.SectionName).Get<HangfireHostOptions>()
                      ?? new HangfireHostOptions();
        if (!options.Enabled)
            return app;

        app.UseHangfireDashboard(options.DashboardPath, new DashboardOptions
        {
            Authorization = [new HangfireAuthorizationFilter()]
        });

        RecurringJob.AddOrUpdate<RefreshTokenCleanupJob>(
            "refresh-token-cleanup",
            job => job.ExecuteAsync(),
            options.RefreshTokenCleanupCron);

        return app;
    }
}
