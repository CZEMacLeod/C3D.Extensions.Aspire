namespace WaitForConsole.WebApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        var app = builder.Build();
        app.MapDefaultEndpoints();
        var magicNumber = builder.Configuration.GetValue<int?>("MAGIC_NUMBER");
        var magicString = magicNumber.HasValue ? $"The magic number is {magicNumber}" : "The magic number was not set";
        app.MapGet("/", () => magicString);
        app.MapGet("/number", () => magicNumber.HasValue ? Results.Ok(magicNumber) : Results.NotFound());

        app.Run();
    }
}
