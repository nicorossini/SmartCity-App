namespace SmartCity.Interfaces.Models;

[GenerateSerializer]
[Alias("Water_Service.Interfaces.Models.WaterSensorData")]
public record WaterSensorData
{
    [Id(0)]
    public string SensorId { get; init; } = string.Empty;
    [Id(1)]
    public string Location { get; init; } = string.Empty;
    [Id(2)]
    public SensorType Type { get; init; }
    [Id(3)]
    public double FlowRate { get; init; } // L/min
    [Id(4)]
    public double Pressure { get; init; } // PSI
    [Id(5)]
    public double Temperature { get; init; } // °C
    [Id(6)]
    public double pH { get; init; }
    [Id(7)]
    public double TurbidityNTU { get; init; } // Turbidity units
    [Id(8)]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    [Id(9)]
    public bool IsAnomalous { get; init; }
    [Id(10)]
    public string ZoneId { get; init; } = string.Empty;
}

public enum SensorType
{
    FlowMeter,
    PressureSensor,
    QualitySensor,
    Temperature,
    Mixed
}