using System.CommandLine;

namespace Koinon.TestData;

/// <summary>
/// Test data generation tool for Koinon RMS
/// Generates realistic seed data for development and testing
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Koinon RMS Test Data Generator");

        // Seed command
        var seedCommand = new Command("seed", "Generate seed data");
        var connectionOption = new Option<string>(
            "--connection",
            description: "Database connection string",
            getDefaultValue: () => "Host=localhost;Port=5432;Database=koinon;Username=koinon;Password=koinon"
        );
        var sizeOption = new Option<string>(
            "--size",
            description: "Dataset size (small, medium, large)",
            getDefaultValue: () => "small"
        );
        var clearOption = new Option<bool>(
            "--clear",
            description: "Clear existing data before seeding",
            getDefaultValue: () => false
        );

        seedCommand.AddOption(connectionOption);
        seedCommand.AddOption(sizeOption);
        seedCommand.AddOption(clearOption);

        seedCommand.SetHandler(async (connection, size, clear) =>
        {
            await SeedData(connection, size, clear);
        }, connectionOption, sizeOption, clearOption);

        rootCommand.AddCommand(seedCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task SeedData(string connection, string size, bool clear)
    {
        Console.WriteLine($"🌱 Seeding {size} dataset...");
        Console.WriteLine($"📊 Connection: {connection}");
        Console.WriteLine($"🗑️  Clear existing: {clear}");
        Console.WriteLine();

        // TODO: Once entities are created, implement data seeding
        // This is a placeholder implementation

        var counts = size.ToLowerInvariant() switch
        {
            "small" => (Families: 50, People: 150, Groups: 10),
            "medium" => (Families: 200, People: 700, Groups: 30),
            "large" => (Families: 1000, People: 3500, Groups: 100),
            _ => throw new ArgumentException($"Invalid size: {size}")
        };

        Console.WriteLine($"📝 Planned generation:");
        Console.WriteLine($"   Families: {counts.Families}");
        Console.WriteLine($"   People:   {counts.People}");
        Console.WriteLine($"   Groups:   {counts.Groups}");
        Console.WriteLine();

        Console.WriteLine("⚠️  Implementation pending: waiting for entity layer (WU-1.2.x)");
        Console.WriteLine("   This tool will be completed after:");
        Console.WriteLine("   - Person entity (WU-1.2.4)");
        Console.WriteLine("   - Group/Family entities (WU-1.2.6-7)");
        Console.WriteLine("   - DbContext setup (WU-1.3.1)");

        await Task.CompletedTask;
    }
}
