namespace Avancira.Infrastructure.Jobs;

public class HangfireOptions
{
    public bool Enabled { get; set; } = false;
    public bool EnableServer { get; set; } = false;
    public bool EnableDashboard { get; set; } = false;
    public string StorageProvider { get; set; } = HangfireStorageProviders.Memory;
    public string? StorageConnectionString { get; set; }
        = null;
    public string? Schema { get; set; }
        = null;
    public string[] Queues { get; set; } = new[] { "default", "email" };
    public int WorkerCount { get; set; } = 5;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public int SchedulePollingIntervalSeconds { get; set; } = 30;
    public string UserName { get; set; } = "admin";
    public string Password { get; set; } = "Secure1234!Me";
    public string Route { get; set; } = "/jobs";
}

public static class HangfireStorageProviders
{
    public const string Memory = "MEMORY";
}
