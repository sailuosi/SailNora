namespace SailNora.Foundation.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>本地文件根目录；相对路径相对进程目录解析。</summary>
    public string RootPath { get; set; } = "App_Data/files";
}
