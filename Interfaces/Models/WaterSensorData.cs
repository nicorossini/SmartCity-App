namespace SmartCity.Interfaces.Models;

public record WaterSensorData
{
    public string SensorId { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public SensorType Type { get; init; }
    public double FlowRate { get; init; } // L/min
    public double Pressure { get; init; } // PSI
    public double Temperature { get; init; } // °C
    public double pH { get; init; }
    public double TurbidityNTU { get; init; } // Turbidity units
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool IsAnomalous { get; init; }
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