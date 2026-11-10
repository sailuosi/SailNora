using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Sandbox.DBLayer;
using SailNora.Product.Sandbox.Dtos;

namespace SailNora.Product.Sandbox.Service;

[Scoped]
public sealed class SandboxRuntimeService
{
    private readonly MooRepository<KnSandboxConfig> _configs;
    private readonly HostSandboxRunner _host;
    private readonly DockerSandboxRunner _docker;

    public SandboxRuntimeService(
        MooRepository<KnSandboxConfig> configs,
        HostSandboxRunner host,
        DockerSandboxRunner docker)
    {
        _configs = configs;
        _host = host;
        _docker = docker;
    }

    public async Task<SandboxCommandResultDto> ExecAsync(
        Guid spaceId,
        string configId,
        RunSandboxCommandInput input,
        CancellationToken cancellationToken = default)
    {
        var c = RequireOwned(spaceId, configId);
        if (!c.Enabled)
            return Fail(c.Backend, "沙箱配置未启用");

        string workDir;
        try
        {
            workDir = HostSandboxRunner.ResolveWorkDir(spaceId, c.WorkDir, input.WorkDir);
        }
        catch (Exception ex)
        {
            return Fail(c.Backend, ex.Message);
        }

        if (string.Equals(c.Backend, "host", StringComparison.OrdinalIgnoreCase))
        {
            var result = await _host.RunAsync(
                workDir, input.Command, input.TimeoutSec, input.MaxOutputBytes, cancellationToken);
            return Map(result, "host", workDir);
        }

        if (string.Equals(c.Backend, "docker", StringComparison.OrdinalIgnoreCase))
        {
            var image = string.IsNullOrWhiteSpace(c.Image) ? "alpine:3.20" : c.Image.Trim();
            var result = await _docker.RunAsync(
                image, workDir, input.Command, input.TimeoutSec, input.MaxOutputBytes, cancellationToken);
            return Map(result, "docker", workDir);
        }

        return new SandboxCommandResultDto
        {
            Success = false,
            Backend = c.Backend,
            ExitCode = -1,
            Error = $"{c.Backend} backend runtime not wired (F5h docker|host only)"
        };
    }

    public async Task<SandboxProbeResultDto> ProbeHostAsync(Guid spaceId, KnSandboxConfig c, CancellationToken ct)
    {
        if (!c.Enabled)
            return new SandboxProbeResultDto { Ok = false, Backend = c.Backend, Message = "disabled" };

        if (string.Equals(c.Backend, "remote", StringComparison.OrdinalIgnoreCase))
            return new SandboxProbeResultDto
            {
                Ok = c.Enabled,
                Backend = c.Backend,
                Message = "remote backend registered (runtime not wired)"
            };

        var workDir = HostSandboxRunner.ResolveWorkDir(spaceId, c.WorkDir, null);
        Directory.CreateDirectory(workDir);

        if (string.Equals(c.Backend, "docker", StringComparison.OrdinalIgnoreCase))
        {
            if (!_docker.IsDockerAvailable())
                return new SandboxProbeResultDto
                {
                    Ok = false,
                    Backend = "docker",
                    Message = "docker not available"
                };

            var image = string.IsNullOrWhiteSpace(c.Image) ? "alpine:3.20" : c.Image.Trim();
            var result = await _docker.RunAsync(image, workDir, "echo ok", timeoutSec: 30, maxOutputBytes: 1024, ct);
            return new SandboxProbeResultDto
            {
                Ok = result.Success && result.ExitCode == 0 && result.Stdout.Contains("ok", StringComparison.OrdinalIgnoreCase),
                Backend = "docker",
                Message = result.Success
                    ? $"docker ok image={image} duration_ms={result.DurationMs}"
                    : result.Error ?? "docker probe failed"
            };
        }

        var hostResult = await _host.RunAsync(workDir, "echo ok", timeoutSec: 10, maxOutputBytes: 1024, ct);
        return new SandboxProbeResultDto
        {
            Ok = hostResult.Success && hostResult.ExitCode == 0 && hostResult.Stdout.Contains("ok", StringComparison.OrdinalIgnoreCase),
            Backend = "host",
            Message = hostResult.Success
                ? $"host ok workdir={workDir} duration_ms={hostResult.DurationMs}"
                : hostResult.Error ?? "host probe failed"
        };
    }

    private KnSandboxConfig RequireOwned(Guid spaceId, string id)
    {
        var c = _configs.Repo.GetById(id)
                ?? throw new InvalidOperationException("沙箱配置不存在");
        if (c.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");
        return c;
    }

    private static SandboxCommandResultDto Map(HostExecResult result, string backend, string workDir) => new()
    {
        Success = result.Success,
        Backend = backend,
        ExitCode = result.ExitCode,
        Stdout = result.Stdout,
        Stderr = result.Stderr,
        Killed = result.Killed,
        DurationMs = result.DurationMs,
        Error = result.Error,
        WorkDir = workDir
    };

    private static SandboxCommandResultDto Fail(string backend, string error) => new()
    {
        Success = false,
        Backend = backend,
        ExitCode = -1,
        Error = error
    };
}
