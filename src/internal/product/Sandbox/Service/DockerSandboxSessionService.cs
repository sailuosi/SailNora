using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Sandbox.DBLayer;
using SailNora.Product.Sandbox.Dtos;

namespace SailNora.Product.Sandbox.Service;

/// <summary>F5m：Docker 长生命周期会话（docker run -d / exec / rm）。</summary>
public sealed class DockerSandboxSessionService : ISingleton
{
    private readonly ConcurrentDictionary<string, DockerSandboxSession> _sessions = new(StringComparer.Ordinal);

    public IReadOnlyList<DockerSandboxSession> ListForConfig(Guid spaceId, string configId) =>
        _sessions.Values
            .Where(s => s.SpaceId == spaceId && s.ConfigId == configId && !s.Destroyed)
            .OrderByDescending(s => s.CreateTime)
            .ToList();

    public async Task<SandboxSessionDto> CreateAsync(
        Guid spaceId,
        KnSandboxConfig config,
        CreateSandboxSessionInput? input,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(config.Backend, "docker", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("长会话仅支持 backend=docker");
        if (!config.Enabled)
            throw new InvalidOperationException("沙箱配置未启用");
        if (!IsDockerAvailable())
            throw new InvalidOperationException("docker not available");

        var image = string.IsNullOrWhiteSpace(config.Image) ? "alpine:3.20" : config.Image.Trim();
        var workDir = HostSandboxRunner.ResolveWorkDir(spaceId, config.WorkDir, input?.WorkDir);
        Directory.CreateDirectory(workDir);
        var fullWorkDir = Path.GetFullPath(workDir);

        var sessionId = Guid.NewGuid().ToString("N");
        var name = "sailnora-" + sessionId[..12];
        // 长驻：sleep infinity；安全：--network none；绑定工作目录
        var args = new StringBuilder();
        args.Append("run -d --name ").Append(Quote(name)).Append(' ');
        args.Append("--network none ");
        args.Append("-w /workspace ");
        args.Append("-v \"").Append(fullWorkDir.Replace("\"", "")).Append(":/workspace:rw\" ");
        args.Append(Quote(image)).Append(' ');
        args.Append("sleep infinity");

        var (ok, stdout, stderr, exit) = await RunDockerAsync(args.ToString(), timeoutSec: 60, cancellationToken);
        if (!ok || exit != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);

        var containerId = stdout.Trim();
        if (string.IsNullOrWhiteSpace(containerId))
            throw new InvalidOperationException("docker run -d 未返回 container id");

        var entity = new DockerSandboxSession
        {
            Id = sessionId,
            SpaceId = spaceId,
            ConfigId = config.Id,
            ContainerId = containerId,
            ContainerName = name,
            Image = image,
            WorkDir = fullWorkDir,
            CreateTime = DateTime.Now
        };
        _sessions[sessionId] = entity;
        return ToDto(entity);
    }

    public async Task<SandboxCommandResultDto> ExecAsync(
        Guid spaceId,
        string configId,
        string sessionId,
        RunSandboxCommandInput input,
        CancellationToken cancellationToken = default)
    {
        var session = RequireSession(spaceId, configId, sessionId);
        if (string.IsNullOrWhiteSpace(input.Command))
            return Fail("command 不能为空");
        if (input.Command.Length > HostSandboxRunner.MaxCommandLength)
            return Fail($"command 过长（>{HostSandboxRunner.MaxCommandLength}）");
        if (!IsDockerAvailable())
            return Fail("docker not available");

        var timeout = Math.Clamp(input.TimeoutSec ?? HostSandboxRunner.DefaultTimeoutSec, 1, HostSandboxRunner.MaxTimeoutSec);
        var maxBytes = Math.Clamp(input.MaxOutputBytes ?? HostSandboxRunner.DefaultMaxOutputBytes, 256, HostSandboxRunner.MaxOutputBytesCap);

        var args = new StringBuilder();
        args.Append("exec ").Append(Quote(session.ContainerName)).Append(' ');
        if (OperatingSystem.IsWindows())
            args.Append("cmd /c ").Append(Quote(input.Command));
        else
            args.Append("/bin/sh -c ").Append(Quote(input.Command));

        var sw = Stopwatch.StartNew();
        var (ok, stdout, stderr, exit) = await RunDockerAsync(args.ToString(), timeout, cancellationToken);
        sw.Stop();
        return new SandboxCommandResultDto
        {
            Success = ok,
            Backend = "docker-session",
            ExitCode = exit,
            Stdout = Truncate(stdout, maxBytes),
            Stderr = Truncate(stderr, maxBytes),
            Killed = !ok && stderr.Contains("timeout", StringComparison.OrdinalIgnoreCase),
            DurationMs = sw.ElapsedMilliseconds,
            Error = ok ? null : (string.IsNullOrWhiteSpace(stderr) ? "docker exec failed" : stderr),
            WorkDir = session.WorkDir
        };
    }

    public async Task<SandboxSessionDto> DestroyAsync(
        Guid spaceId,
        string configId,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = RequireSession(spaceId, configId, sessionId);
        if (IsDockerAvailable())
        {
            _ = await RunDockerAsync(
                "rm -f " + Quote(session.ContainerName),
                timeoutSec: 30,
                cancellationToken);
        }

        session.Destroyed = true;
        _sessions.TryRemove(sessionId, out _);
        return ToDto(session);
    }

    private DockerSandboxSession RequireSession(Guid spaceId, string configId, string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var s) || s.Destroyed)
            throw new InvalidOperationException("沙箱会话不存在");
        if (s.SpaceId != spaceId || s.ConfigId != configId)
            throw new UnauthorizedAccessException("无权访问");
        return s;
    }

    public static bool IsDockerAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "version --format {{.Server.Version}}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc is null)
                return false;
            if (!proc.WaitForExit(5000))
            {
                try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return false;
            }
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(bool Ok, string Stdout, string Stderr, int ExitCode)> RunDockerAsync(
        string arguments,
        int timeoutSec,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        try
        {
            using var proc = new Process { StartInfo = psi };
            if (!proc.Start())
                return (false, "", "无法启动 docker", -1);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(TimeSpan.FromSeconds(timeoutSec));
            var stdoutTask = proc.StandardOutput.ReadToEndAsync(linked.Token);
            var stderrTask = proc.StandardError.ReadToEndAsync(linked.Token);
            try
            {
                await proc.WaitForExitAsync(linked.Token);
                return (true, await stdoutTask, await stderrTask, proc.ExitCode);
            }
            catch (OperationCanceledException)
            {
                try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return (false, "", cancellationToken.IsCancellationRequested ? "cancelled" : $"timeout after {timeoutSec}s", -1);
            }
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message, -1);
        }
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private static string Truncate(string text, int maxBytes)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        var bytes = Encoding.UTF8.GetByteCount(text);
        if (bytes <= maxBytes)
            return text;
        return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(text), 0, maxBytes) + "\n…[truncated]";
    }

    private static SandboxCommandResultDto Fail(string error) => new()
    {
        Success = false,
        Backend = "docker-session",
        ExitCode = -1,
        Error = error
    };

    private static SandboxSessionDto ToDto(DockerSandboxSession s) => new()
    {
        Id = s.Id,
        ConfigId = s.ConfigId,
        ContainerId = s.ContainerId,
        ContainerName = s.ContainerName,
        Image = s.Image,
        WorkDir = s.WorkDir,
        Destroyed = s.Destroyed,
        CreateTime = s.CreateTime
    };
}

public sealed class DockerSandboxSession
{
    public string Id { get; set; } = string.Empty;
    public Guid SpaceId { get; set; }
    public string ConfigId { get; set; } = string.Empty;
    public string ContainerId { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string WorkDir { get; set; } = string.Empty;
    public bool Destroyed { get; set; }
    public DateTime CreateTime { get; set; }
}
