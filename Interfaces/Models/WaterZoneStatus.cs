namespace SmartCity.Interfaces.Models;
public record WaterZoneStatus
{
    public string ZoneId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public double TotalFlowRate { get; init; }
    public double AveragePressure { get; init; }
    public double WaterQualityIndex { get; init; }
    public List<string> ActiveSensors { get; init; } = new();
    public List<WaterAlert> ActiveAlerts { get; init; } = new();
    public DateTime LastUpdate { get; init; } = DateTime.UtcNow;
    public ZoneStatus Status { get; init; }
}

public enum ZoneStatus
{
    Normal,
    LowPressure,
    HighPressure,
    QualityIssue,
    LeakDetected,
    Maintenance
}