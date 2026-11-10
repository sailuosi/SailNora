namespace SailNora.Product.Sandbox.Dtos;

public sealed class CreateSandboxConfigInput
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Backend { get; set; } = "host";
    public string? Image { get; set; }
    public string? WorkDir { get; set; }
}

public sealed class UpdateSandboxConfigInput
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Backend { get; set; }
    public string? Image { get; set; }
    public string? WorkDir { get; set; }
    public bool? Enabled { get; set; }
}

public sealed class SandboxConfigDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Backend { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string? WorkDir { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public sealed class SandboxInventoryDto
{
    public string ConfigId { get; set; } = string.Empty;
    public int ActiveCount { get; set; }
    public string[] Instances { get; set; } = [];
    public string Note { get; set; } = "F5d stub: no live sandbox runtime";
}

public sealed class SandboxProbeResultDto
{
    public bool Ok { get; set; }
    public string Backend { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class RunSandboxCommandInput
{
    public string Command { get; set; } = string.Empty;
    public string? WorkDir { get; set; }
    public int? TimeoutSec { get; set; }
    public int? MaxOutputBytes { get; set; }
}

public sealed class SandboxCommandResultDto
{
    public bool Success { get; set; }
    public string Backend { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public string Stdout { get; set; } = string.Empty;
    public string Stderr { get; set; } = string.Empty;
    public bool Killed { get; set; }
    public long DurationMs { get; set; }
    public string? Error { get; set; }
    public string? WorkDir { get; set; }
}

public sealed class CreateSandboxSessionInput
{
    public string? WorkDir { get; set; }
}

public sealed class SandboxSessionDto
{
    public string Id { get; set; } = string.Empty;
    public string ConfigId { get; set; } = string.Empty;
    public string ContainerId { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string WorkDir { get; set; } = string.Empty;
    public bool Destroyed { get; set; }
    public DateTime CreateTime { get; set; }
}
