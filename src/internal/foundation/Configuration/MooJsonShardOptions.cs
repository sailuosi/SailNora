namespace SailNora.Foundation.Configuration;

public sealed class MooJsonShardOptions
{
    public List<string> ScanDirectories { get; } = [];

    public List<string> IgnoreFiles { get; } = [];

    public bool ReloadOnChange { get; set; } = true;
}
