namespace SailNora.Foundation.Options;

/// <summary>宿主 Hangfire 开关（与 Configuration/Hangfire.json 对齐）。</summary>
public class HangfireHostOptions
{
    public const string SectionName = "Hangfire";

    public bool Enabled { get; set; } = true;

    public string DashboardPath { get; set; } = "/hangfire";

    public string TablesPrefix { get; set; } = "hf_";

    public string RefreshTokenCleanupCron { get; set; } = "0 3 * * *";
}
