using System;

namespace MaNoir.Core.Contracts.Models.Mesh;

public enum WeatherInfoKind
{
    Sunny,
    Fair,
    Cloudy,
    Rain,
    HardRain,
    Snow,
    Fog
}

public sealed class WeatherInfo
{
    public DateTimeOffset DateDebut { get; set; }
    public DateTimeOffset DateFin { get; set; }
    public string Label { get; set; }
    public WeatherInfoKind Kind { get; set; }
    public int Temperature { get; set; }
    public int TemperatureFeltAs { get; set; }
    public bool RiskOfThumber { get; set; }

    public static string GetLabelFromKind(WeatherInfoKind kind) => kind.ToString();
}

public enum WeatherHazardSeverity
{
    Mild,
    Moderate,
    Important
}

public enum WeatherHazardKind
{
    Other = 0,
    Wind = 1,
    HardRain,
    Storm,
    Flood,
    Snow,
    HighTemperature,
    LowTemperature
}

public sealed class WeatherHazard
{
    public DateTimeOffset DateDebut { get; set; }
    public DateTimeOffset DateFin { get; set; }
    public WeatherHazardKind Kind { get; set; }
    public WeatherHazardSeverity Severity { get; set; }
    public string Label { get; set; }
}