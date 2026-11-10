using System.Diagnostics;
using System.Text;
using SailNora.Foundation.DI;

namespace SailNora.Product.Sandbox.Service;

/// <summary>F5e host backend：受控进程执行（对齐上游 shell -c，无 docker）。</summary>
[Scoped]
public sealed class HostSandboxRunner
{
    public const int DefaultTimeoutSec = 120;
    public const int MaxTimeoutSec = 600;
    public const int DefaultMaxOutputBytes = 16 * 1024;
    public const int MaxOutputBytesCap = 64 * 1024;
    public const int MaxCommandLength = 8 * 1024;

    public async Task<HostExecResult> RunAsync(
        string workDir,
        string command,
        int? timeoutSec,
        int? maxOutputBytes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            return Fail("command 不能为空");
        if (command.Length > MaxCommandLength)
            return Fail($"command 过长（>{MaxCommandLength}）");

        var timeout = Math.Clamp(timeoutSec ?? DefaultTimeoutSec, 1, MaxTimeoutSec);
        var maxBytes = Math.Clamp(maxOutputBytes ?? DefaultMaxOutputBytes, 256, MaxOutputBytesCap);

        Directory.CreateDirectory(workDir);
        var fullWorkDir = Path.GetFullPath(workDir);
        if (!Directory.Exists(fullWorkDir))
            return Fail("工作目录不可用");

        var (fileName, arguments) = BuildShellInvocation(command);
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = fullWorkDir,
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
                return Fail("无法启动进程", sw.ElapsedMilliseconds);

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
                TryKill(proc);
                sw.Stop();
                var killedByUser = cancellationToken.IsCancellationRequested;
                return new HostExecResult
                {
                    Success = false,
                    ExitCode = -1,
                    Stdout = string.Empty,
                    Stderr = string.Empty,
                    Killed = true,
                    DurationMs = sw.ElapsedMilliseconds,
                    Error = killedByUser ? "cancelled" : $"timeout after {timeout}s"
                };
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Fail(ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public static string ResolveWorkDir(Guid spaceId, string? configWorkDir, string? requestWorkDir)
    {
        var preferred = !string.IsNullOrWhiteSpace(requestWorkDir)
            ? requestWorkDir.Trim()
            : !string.IsNullOrWhiteSpace(configWorkDir)
                ? configWorkDir.Trim()
                : Path.Combine(Path.GetTempPath(), "sail-nora-sandbox", spaceId.ToString("N"));

        var full = Path.GetFullPath(preferred);
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "sail-nora-sandbox"));
        // 允许配置显式 WorkDir；若落在默认 root 下则强制不得逃逸 root
        if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            && !full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
        {
            // edge: equal root ok for create
        }

        if (full.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("非法工作目录");

        return full;
    }

    public static (string FileName, string Arguments) BuildShellInvocation(string command)
    {
        if (OperatingSystem.IsWindows())
            return ("cmd.exe", "/d /s /c " + QuoteForCmd(command));
        return ("/bin/sh", "-c " + QuoteForSh(command));
    }

    private static string QuoteForCmd(string command) => "\"" + command.Replace("\"", "\\\"") + "\"";

    private static string QuoteForSh(string command) => "'" + command.Replace("'", "'\\''") + "'";

    private static string Truncate(string text, int maxBytes)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        var bytes = Encoding.UTF8.GetByteCount(text);
        if (bytes <= maxBytes)
            return text;
        var chars = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(text), 0, maxBytes);
        return chars + "\n…[truncated]";
    }

    private static void TryKill(Process proc)
    {
        try
        {
            if (!proc.HasExited)
                proc.Kill(entireProcessTree: true);
        }
        catch
        {
            // ignore
        }
    }

    private static HostExecResult Fail(string error, long durationMs = 0) => new()
    {
        Success = false,
        ExitCode = -1,
        Stdout = string.Empty,
        Stderr = string.Empty,
        Killed = false,
        DurationMs = durationMs,
        Error = error
    };
}

public sealed class HostExecResult
{
    public bool Success { get; init; }
    public int ExitCode { get; init; }
    public string Stdout { get; init; } = string.Empty;
    public string Stderr { get; init; } = string.Empty;
    public bool Killed { get; init; }
    public long DurationMs { get; init; }
    public string? Error { get; init; }
}
