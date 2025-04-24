using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Principal;

namespace C3D.Extensions.Aspire.IISExpress;

internal class CommandExecutor
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

    public async Task<int> ExecuteAdminCommandAsync(string cmd, params IEnumerable<string> args) => 
        IsAdmin ? await ExecuteCommandAsync(cmd, args) : 
            await ExecuteCommandAsync("sudo", args.Prepend(cmd).Prepend("--inline"));   // Requires Sudo for Windows

    public async Task<int> ExecuteCommandAsync(string cmd, params IEnumerable<string> args)
    {
        var proc = new ProcessStartInfo(cmd, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var argsString = string.Join(" ", args.Select(a => CensoredArgs.Contains(a) ? "****" : a));
        logger.LogInformation("Running command: {Command} {Args}", cmd, argsString);

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
            var logOutput = CensoredArgs.Aggregate(output!, (o, a) => o.Replace(a, "****"));
            logger.LogInformation("Command output: {Output}", logOutput);
        }
        if (!string.IsNullOrEmpty(error))
        {
            var logError = CensoredArgs.Aggregate(error!, (o, a) => o.Replace(a, "****"));
            logger.LogWarning("Command error: {Error}", logError);
        }
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
