using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using YamlDotNet.Core.Tokens;

namespace C3D.Extensions.Aspire.IISExpress;

public class CommandExecutor
{
    private readonly ILogger<CommandExecutor> logger;

    public CommandExecutor(ILogger<CommandExecutor> logger)
    {
        this.IsAdmin = IsAdministrator();
        this.logger = logger;
    }

    public bool IsAdmin { get; }

    public List<string> CensoredArgs { get; } = new();


    private static bool IsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        if (identity == null)
        {
            return false;
        }
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public async Task<int> ExecuteAdminCommandAsync(string cmd, ILogger logger, params IEnumerable<string> args) => 
        IsAdmin ? await ExecuteCommandAsync(cmd, args) : 
            await ExecuteCommandAsync("sudo", logger, args.Prepend(cmd).Prepend("--inline"));   // Requires Sudo for Windows

    public Task<int> ExecuteAdminCommandAsync(string cmd, params IEnumerable<string> args) =>
        ExecuteAdminCommandAsync(cmd, this.logger, args);

    public Task<int> ExecuteCommandAsync(string cmd, params IEnumerable<string> args) =>
        ExecuteCommandAsync(cmd, this.logger, args);

    public Task<int> ExecuteCommandAsync(string cmd, ILogger logger, params IEnumerable<string> args) =>
        ExecuteCommandAsync(cmd, logger, CancellationToken.None, null, args);

    public async Task<int> ExecuteCommandAsync(string cmd, ILogger logger, CancellationToken cancellationToken, IDictionary<string, string?>? envVars, params IEnumerable<string> args)
    {
        var proc = new ProcessStartInfo(cmd, args)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        if (envVars != null)
        {
            foreach (var kvp in envVars)
            {
                if (kvp.Value == null)
                {
                    proc.Environment.Remove(kvp.Key);
                    continue;
                }
                proc.Environment[kvp.Key] = kvp.Value;
            }
        }

        var argsString = string.Join(" ", args.Select(a => CensoredArgs.Contains(a) ? "****" : a));
        logger.LogInformation("Running command: {Command} {Args}", cmd, argsString);

        var sbOutput = new StringBuilder();
        var sbErrors = new StringBuilder();

        using var process = new Process
        {
            StartInfo = proc,
            EnableRaisingEvents = true
        };
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                var logOutput = CensoredArgs.Aggregate(e.Data!, (o, a) => o.Replace(a, "****"));
                sbOutput.AppendLine(logOutput);
                //logger.LogInformation("Command {Command} output: {Output}", cmd, logOutput);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                var logError = CensoredArgs.Aggregate(e.Data!, (o, a) => o.Replace(a, "****"));
                sbErrors.AppendLine(logError);
                //logger.LogWarning("Command {Command} error: {Error}", cmd, logError);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var id = process.Id;
        var processName = process.ProcessName;
        logger.LogInformation("Started command {Command} with PID {pid}", cmd, id);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        //linkedCts.Token.Register(() =>
        //{
        //    if (!process.HasExited)
        //    {
        //        logger.LogWarning("Command {Command} timed out after 60 seconds, killing process {pid}", cmd, process.Id);
        //        process.Kill();
        //    }
        //});
        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
            if (timeoutCts.IsCancellationRequested)
            {
                logger.LogWarning("Command {ProcessName}:{PID} timed out after 60 seconds", processName, id);
            }
            else
            {
                logger.LogWarning("Command {ProcessName}:{PID} was cancelled", processName, id);
            }
            if (!process.HasExited)
            {
                process.Kill();
            }
        }

        if (process.ExitCode != 0)
        {
            logger.LogInformation("Command {ProcessName}:{PID} exited with code {ExitCode}", processName, id, process.ExitCode);
        }

        //if (process.StartInfo.RedirectStandardOutput)
        //{
        //    var output = process.StandardOutput.ReadToEnd();
        if (sbOutput.Length!=0)
        {
            logger.LogInformation("Command {ProcessName}:{PID} output: {Output}", processName, id, sbOutput);
        }
        //}
        //if (process.StartInfo.RedirectStandardError)
        //{
        //    var error = process.StandardError.ReadToEnd();
        if (sbErrors.Length!=0)
        {
            logger.LogWarning("Command {ProcessName}:{PID} error: {Error}", processName, process.Id, sbErrors);
        }
        //}

        return process.ExitCode;
    }

    [Obsolete("Use ExecuteCommandAsync(string cmd, params IEnumerable<string> args) or ExecuteAdminCommandAsync(string cmd, params IEnumerable<string> args) instead.")]
    public async Task<int> ExecuteCommandAsync(string cmd, bool requiresAdmin, string args)
    {
        var proc = new ProcessStartInfo(cmd, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (requiresAdmin && !IsAdmin)
        {
            proc.FileName = "sudo"; // Relies on Sudo for Windows
            proc.Arguments = $"--inline {cmd} {args}";
        }
        logger.LogInformation("Running command: {Command} {Args}", cmd, args);
        using var process = new Process { StartInfo = proc };
        process.Start();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            logger.LogInformation("Command {Command} exited with code {ExitCode}", cmd, process.ExitCode);
        }
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        if (!string.IsNullOrEmpty(output))
        {
            logger.LogInformation("Command output: {Output}", output);
        }
        if (!string.IsNullOrEmpty(error))
        {
            logger.LogWarning("Command error: {Error}", error);
        }
        return process.ExitCode;
    }

    public async Task<(int exitCode, string output)> GetCommandOutputAsync(string cmd, string args, bool runAs)
    {
        logger.LogInformation("Running command: {Command} {Args}", cmd, args);
        var proc = new ProcessStartInfo(cmd, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (runAs)
        {
            proc.Verb = "runas";
        }
        using var process = new Process { StartInfo = proc };
        process.Start();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            logger.LogInformation("Command {Command} exited with code {ExitCode}", cmd, process.ExitCode);
        }
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        if (!string.IsNullOrEmpty(output))
        {
            logger.LogInformation("Command output: {Output}", output);
        }
        if (!string.IsNullOrEmpty(error))
        {
            logger.LogWarning("Command error: {Error}", error);
        }
        return (process.ExitCode, output);
    }
}
