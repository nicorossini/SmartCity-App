namespace SmartCity.Interfaces.Models;
public record WaterAlert
{
    public string AlertId { get; init; } = Guid.NewGuid().ToString();
    public AlertType Type { get; init; }
    public string SensorId { get; init; } = string.Empty;
    public string ZoneId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public AlertSeverity Severity { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool IsResolved { get; init; }
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