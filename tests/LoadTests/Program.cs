using LoadTests;
using LoadTests.Scenarios;
using Microsoft.Extensions.Configuration;
using NBomber.Contracts;
using NBomber.CSharp;

// Load configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development"}.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var config = configuration.Get<LoadTestConfig>() ?? new LoadTestConfig();

// Parse command line arguments
var scenario = args.Length > 0 ? args[0].ToLowerInvariant() : "normal";
var variant = args.Length > 1 ? args[1].ToLowerInvariant() : "default";

Console.WriteLine("===========================================");
Console.WriteLine("     Micro-Commerce Load Testing Suite     ");
Console.WriteLine("===========================================");
Console.WriteLine();
Console.WriteLine($"Catalog API: {config.BaseUrls.CatalogApi}");
Console.WriteLine($"Order API:   {config.BaseUrls.OrderApi}");
Console.WriteLine($"Gateway:     {config.BaseUrls.Gateway}");
Console.WriteLine();
Console.WriteLine($"Selected scenario: {scenario}");
Console.WriteLine($"Variant: {variant}");
Console.WriteLine();

var scenarioToRun = GetScenario(scenario, variant, config);

if (scenarioToRun is null)
{
    PrintUsage();
    return 1;
}

// Run the load test
NBomberRunner
    .RegisterScenarios(scenarioToRun)
    .WithReportFolder(config.Reporting.ReportFolder)
    .WithReportFileName(config.Reporting.ReportFileName)
    .Run();

return 0;

static ScenarioProps? GetScenario(string scenario, string variant, LoadTestConfig config)
{
    return (scenario, variant) switch
    {
        // Normal load scenarios
        ("normal", "default") => NormalLoadScenario.Create(config),
        ("normal", "orders") => NormalLoadScenario.CreateWithOrders(config),
        
        // Peak load scenarios
        ("peak", "default") => PeakLoadScenario.Create(config),
        ("peak", "rampup") => PeakLoadScenario.CreateWithRampUp(config),
        
        // Stress test scenarios
        ("stress", "default") => StressTestScenario.Create(config),
        ("stress", "writes") => StressTestScenario.CreateWithWriteOperations(config),
        ("stress", "concurrent") => StressTestScenario.CreateConcurrentUsers(config),
        
        // Spike test scenarios
        ("spike", "default") => SpikeTestScenario.Create(config),
        ("spike", "double") => SpikeTestScenario.CreateDoubleSpike(config),
        ("spike", "gradual") => SpikeTestScenario.CreateGradualSpike(config),
        
        // Endurance test scenarios
        ("endurance", "default") => EnduranceTestScenario.Create(config),
        ("endurance", "short") => EnduranceTestScenario.CreateShort(config),
        ("endurance", "writes") => EnduranceTestScenario.CreateWithWrites(config),
        ("endurance", "concurrent") => EnduranceTestScenario.CreateConcurrentUsers(config),
        
        // Write operation scenarios (CRUD)
        ("write", "products") => WriteOperationsScenario.CreateProductCrud(config),
        ("write", "categories") => WriteOperationsScenario.CreateCategoryCrud(config),
        ("write", "orders") => WriteOperationsScenario.CreateOrderCrud(config),
        ("write", "mixed") => WriteOperationsScenario.CreateMixedReadWrite(config),
        ("write", "heavy") => WriteOperationsScenario.CreateWriteHeavy(config),
        ("write", "burst") => WriteOperationsScenario.CreateBurstWrites(config),
        
        // All scenarios combined (for comprehensive testing)
        ("all", _) => null, // Special case handled separately
        
        _ => null
    };
}

static void PrintUsage()
{
    Console.WriteLine("Usage: dotnet run <scenario> [variant]");
    Console.WriteLine();
    Console.WriteLine("Available scenarios:");
    Console.WriteLine();
    Console.WriteLine("  normal                 - 100 RPS baseline for 2 minutes");
    Console.WriteLine("    variants: default, orders");
    Console.WriteLine();
    Console.WriteLine("  peak                   - 200 RPS (2x normal) for 3 minutes");
    Console.WriteLine("    variants: default, rampup");
    Console.WriteLine();
    Console.WriteLine("  stress                 - Incrementing load to find breaking point");
    Console.WriteLine("    variants: default, writes, concurrent");
    Console.WriteLine();
    Console.WriteLine("  spike                  - Sudden 500 RPS spike");
    Console.WriteLine("    variants: default, double, gradual");
    Console.WriteLine();
    Console.WriteLine("  endurance              - Sustained 100 RPS for 30 minutes");
    Console.WriteLine("    variants: default, short, writes, concurrent");
    Console.WriteLine();
    Console.WriteLine("  write                  - Write operations (CREATE, UPDATE, DELETE)");
    Console.WriteLine("    variants: products, categories, orders, mixed, heavy, burst");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run normal");
    Console.WriteLine("  dotnet run stress writes");
    Console.WriteLine("  dotnet run endurance short");
    Console.WriteLine("  dotnet run write products    # Test product CRUD");
    Console.WriteLine("  dotnet run write mixed       # 70% reads, 30% writes");
    Console.WriteLine("  dotnet run write burst       # Flash sale simulation");
    Console.WriteLine();
    Console.WriteLine("Environment variables:");
    Console.WriteLine("  CATALOG_API_URL        - Override Catalog API URL");
    Console.WriteLine("  ORDER_API_URL          - Override Order API URL");
    Console.WriteLine("  GATEWAY_URL            - Override Gateway URL");
}
