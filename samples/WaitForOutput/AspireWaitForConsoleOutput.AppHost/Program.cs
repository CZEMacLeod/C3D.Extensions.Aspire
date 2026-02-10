using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

internal partial class Program
{
    private static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // When testing, don't bother with the sql server docker image to speed up tests.
        // These are for demo purposes only and not actually used.
        var sqldb = builder.WhenNotUnderTest(b => b.AddSqlServer("sql").AddDatabase("sqldb"));

        var localFramework = "net" + System.Environment.Version.ToString(2);

        // Example of waiting for a console app to output a specific message
        // This could be an Executable of some sort - such as Node, Python, etc.
        // We wait for some pre-requisite to be ready, then we wait for the console app to output a specific message
        var console = builder.AddProject<Projects.WaitForConsole_ConsoleApp>("consoleapp")
            .WithArgs("-f", localFramework)
            .WhenNotUnderTest(r=>r.WithReference(sqldb!, "sqldb")
                                  .WaitFor(sqldb!)
            )
            .WithOutputWatcher(GetMagic(), true, "magic")
            .CreateReferenceExpression("magic", out var magicNumber)
            .OnMatched((o, c) =>
            {
                var logger = o.ServiceProvider.GetRequiredService<ILogger<Program>>();
                // Get the magic number from the console app
                // The regex match group capture is 'magic' and will be stored as a property.
                if (int.TryParse(o.Properties["magic"].ToString()!, out var number))
                {
                    logger.LogInformation("{number} is the magic number!", number);
                }
                else
                {
                    logger.LogError("Could not parse {magic} as a number", o.Properties["magic"]);
                }
                return Task.CompletedTask;
            });

        // The GetMagic Regex will capture the magic number from the console app output and store it as a property
        // The output string will be of the format "{number} is the magic number!"
        // Once it is detected, we can retrieve it using a ReferenceExpression, as an environment variable in the next project

        // The fluent CreateReferenceExpression method above is just a shortcut for 
        // var magicNumber = console.GetReferenceExpression("magic");

        // webapp won't start until console has output the message "Ready Now..."
        // Note that 'console' does not have to exit, it just has to output the message
        builder.AddProject<Projects.WaitForConsole_WebApp>("webapp")
            .WithArgs("-f", localFramework)
            .WhenNotUnderTest(r => r.WithReference(sqldb!, "sqldb")
                                    .WaitFor(sqldb!)
)           .WaitForOutput(console, m => m == "Ready Now...")
            .WithEnvironment("MAGIC_NUMBER", magicNumber);

        builder.Build().Run();
    }

    [GeneratedRegex("^(?<magic>.*\\d)(?:( is the magic number!))$")]
    public static partial Regex GetMagic();
}

