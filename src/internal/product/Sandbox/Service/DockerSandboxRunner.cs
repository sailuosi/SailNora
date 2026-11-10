using System.Diagnostics;
using System.Text;
using SailNora.Foundation.DI;

namespace SailNora.Product.Sandbox.Service;

/// <summary>F5h：通过 docker CLI 一次性 <c>docker run --rm</c>（无长生命周期会话）。</summary>
public sealed class DockerSandboxRunner : IScoped
{
    public bool IsDockerAvailable()
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

    public async Task<HostExecResult> RunAsync(
        string image,
        string workDir,
        string command,
        int? timeoutSec,
        int? maxOutputBytes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(image))
            return Fail("docker image 未配置");
        if (string.IsNullOrWhiteSpace(command))
            return Fail("command 不能为空");
        if (command.Length > HostSandboxRunner.MaxCommandLength)
            return Fail($"command 过长（>{HostSandboxRunner.MaxCommandLength}）");
        if (!IsDockerAvailable())
            return Fail("docker not available");

        Directory.CreateDirectory(workDir);
        var fullWorkDir = Path.GetFullPath(workDir);
        var timeout = Math.Clamp(timeoutSec ?? HostSandboxRunner.DefaultTimeoutSec, 1, HostSandboxRunner.MaxTimeoutSec);
        var maxBytes = Math.Clamp(maxOutputBytes ?? HostSandboxRunner.DefaultMaxOutputBytes, 256, HostSandboxRunner.MaxOutputBytesCap);

        // 安全：固定 --rm --network none；禁止调用方注入 volume/privileged
        var args = new StringBuilder();
        args.Append("run --rm --network none ");
        args.Append("-w /workspace ");
        args.Append("-v \"").Append(fullWorkDir.Replace("\"", "")).Append(":/workspace:rw\" ");
        args.Append(QuoteArg(image.Trim())).Append(' ');
        if (OperatingSystem.IsWindows())
            args.Append("cmd /c ").Append(QuoteArg(command));
        else
            args.Append("/bin/sh -c ").Append(QuoteArg(command));

        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = args.ToString(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        var sw = Stopwatch.StartNew();
        try
        {
            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            if (!proc.Start())
                return Fail("无法启动 docker", sw.ElapsedMilliseconds);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(TimeSpan.FromSeconds(timeout));
            var stdoutTask = proc.StandardOutput.ReadToEndAsync(linked.Token);
            var stderrTask = proc.StandardError.ReadToEndAsync(linked.Token);

            try
            {
                await proc.WaitForExitAsync(linked.Token);
                var stdout = Truncate(await stdoutTask, maxBytes);
                var stderr = Truncate(await stderrTask, maxBytes);
                sw.Stop();
                return new HostExecResult
                {
                    Success = true,
                    ExitCode = proc.ExitCode,
                    Stdout = stdout,
                    Stderr = stderr,
                    Killed = false,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
            catch (OperationCanceledException)
            {
                try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
                sw.Stop();
                return new HostExecResult
                {
                    Success = false,
                    ExitCode = -1,
                    Killed = true,
                    DurationMs = sw.ElapsedMilliseconds,
                    Error = cancellationToken.IsCancellationRequested ? "cancelled" : $"timeout after {timeout}s"
                };
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Fail(ex.Message, sw.ElapsedMilliseconds);
        }
    }

    private static string QuoteArg(string value) =>
        "\"" + value.Replace("\"", "\\\"") + "\"";

    private static string Truncate(string text, int maxBytes)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        var bytes = Encoding.UTF8.GetByteCount(text);
        if (bytes <= maxBytes)
            return text;
        return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(text), 0, maxBytes) + "\n…[truncated]";
    }

    private static HostExecResult Fail(string error, long durationMs = 0) => new()
    {
        Success = false,
        ExitCode = -1,
        Error = error,
        DurationMs = durationMs
    };
}
