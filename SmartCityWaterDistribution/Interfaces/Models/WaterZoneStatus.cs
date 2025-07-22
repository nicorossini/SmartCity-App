namespace SmartCity.Interfaces.Models;


[GenerateSerializer]
[Alias("Water_Service.Interfaces.Models.WaterZoneStatus")]
public record WaterZoneStatus
{

    [Id(0)]
    public string ZoneId { get; init; } = string.Empty;
    [Id(1)]
    public string Name { get; init; } = string.Empty;
    [Id(2)]
    public double TotalFlowRate { get; init; }
    [Id(3)]
    public double AveragePressure { get; init; }
    [Id(4)]
    public double WaterQualityIndex { get; init; }
    [Id(5)]
    public List<string> ActiveSensors { get; init; } = new();
    [Id(6)]
    public List<WaterAlert> ActiveAlerts { get; init; } = new();
    [Id(7)]
    public DateTime LastUpdate { get; init; } = DateTime.UtcNow;
    [Id(8)]
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