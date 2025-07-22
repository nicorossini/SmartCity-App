namespace SmartCity.Interfaces.Models;

[GenerateSerializer]
[Alias("Water_Service.Interfaces.Models.WaterAlertModel")]
public record WaterAlert
{
    [Id(0)]
    public string AlertId { get; init; } = Guid.NewGuid().ToString();
    [Id(1)]
    public AlertType Type { get; init; }
    [Id(2)]
    public string SensorId { get; init; } = string.Empty;
    [Id(3)]
    public string ZoneId { get; init; } = string.Empty;
    [Id(4)]
    public string Message { get; init; } = string.Empty;
    [Id(5)]
    public AlertSeverity Severity { get; init; }
    [Id(6)]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public enum AlertType
{
    LowPressure,
    HighPressure,
    PoorWaterQuality,
    LeakDetection,
    SensorMalfunction,
    FlowAnomaly
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical,
    Emergency
}
