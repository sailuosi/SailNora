using mooSQL.config;

namespace SailNora.Foundation.Data;

public class DBCoonfigOptions
{
    public const string SectionName = "DBCoonfig";

    public bool EnableConsoleSql { get; set; }

    public List<DBPosition> Connections { get; set; } = [];
}
