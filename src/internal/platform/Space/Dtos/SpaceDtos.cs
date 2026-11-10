namespace SailNora.Platform.Space.Dtos;

public sealed class SpaceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Role { get; set; } = string.Empty;
}

public sealed class CreateSpaceInput
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
}

public sealed class CreateApiKeyInput
{
    public string Name { get; set; } = "default";
}

public sealed class CreateApiKeyResult
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    /// <summary>仅创建时返回明文，之后不可再取。</summary>
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class CapabilitiesDto
{
    public string Edition { get; set; } = "standard";
    public string Backend { get; set; } = "dotnet";
    public bool Agent { get; set; } = true;
    public bool Wiki { get; set; }
    public bool Sandbox { get; set; }
    public bool McpServer { get; set; }
    public bool Datasource { get; set; }
    public bool Embed { get; set; }
    public bool Memory { get; set; }
    public bool Im { get; set; }
    public string[] AuthModes { get; set; } = ["jwt", "api_key"];
}
