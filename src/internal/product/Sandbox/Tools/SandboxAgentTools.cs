using System.Text.Json;
using SailNora.Foundation.DI;
using SailNora.Product.Sandbox.Dtos;
using SailNora.Product.Sandbox.Service;

namespace SailNora.Product.Sandbox.Tools;

/// <summary>对齐上游 shell_exec 工具名。</summary>
[Scoped]
public sealed class SandboxAgentTools
{
    private readonly SandboxRuntimeService _runtime;

    public SandboxAgentTools(SandboxRuntimeService runtime) => _runtime = runtime;

    public async Task<object> ShellExecAsync(
        Guid spaceId,
        string sandboxConfigId,
        string command,
        string? workDir = null,
        int timeoutSec = HostSandboxRunner.DefaultTimeoutSec,
        int maxOutputBytes = HostSandboxRunner.DefaultMaxOutputBytes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sandboxConfigId))
            return new { tool = "shell_exec", success = false, error = "sandbox_config_id 未配置" };

        var result = await _runtime.ExecAsync(
            spaceId,
            sandboxConfigId,
            new RunSandboxCommandInput
            {
                Command = command,
                WorkDir = workDir,
                TimeoutSec = timeoutSec,
                MaxOutputBytes = maxOutputBytes
            },
            cancellationToken);

        // 上游语义：进程跑完（含非 0 exit）success=true；超时/拉起失败 success=false
        var toolSuccess = result.Success;
        return new
        {
            tool = "shell_exec",
            success = toolSuccess,
            exit_code = result.ExitCode,
            stdout = result.Stdout,
            stderr = result.Stderr,
            killed = result.Killed,
            duration_ms = result.DurationMs,
            backend = result.Backend,
            work_dir = result.WorkDir,
            error = result.Error
        };
    }

    public static string ToJson(object value) =>
        JsonSerializer.Serialize(value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
}
