namespace LoadTests;

/// <summary>
/// Configuration for load test scenarios.
/// </summary>
public class LoadTestConfig
{
    public BaseUrlsConfig BaseUrls { get; set; } = new();
    public TestDataConfig TestData { get; set; } = new();
    public ReportingConfig Reporting { get; set; } = new();
}

public class BaseUrlsConfig
{
    public string CatalogApi { get; set; } = "http://localhost:5001";
    public string OrderApi { get; set; } = "http://localhost:5002";
    public string Gateway { get; set; } = "http://localhost:5000";
}

public class TestDataConfig
{
    public string CustomerId { get; set; } = Guid.NewGuid().ToString();
    public string ProductId { get; set; } = Guid.NewGuid().ToString();
    public string CategoryId { get; set; } = Guid.NewGuid().ToString();
}

public class ReportingConfig
{
    public string ReportFolder { get; set; } = "./reports";
    public string ReportFileName { get; set; } = "load_test_report";
    public string[] ReportFormats { get; set; } = ["html", "csv", "txt"];
}
