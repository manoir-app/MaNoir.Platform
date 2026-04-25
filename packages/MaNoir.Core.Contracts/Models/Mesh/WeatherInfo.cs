using System;

namespace MaNoir.Core.Contracts.Models.Mesh;

/// <summary>
/// Describes a weather condition for a time window.
/// </summary>
public enum WeatherInfoKind
{
    /// <summary>
    /// Clear sunny weather.
    /// </summary>
    Sunny,
    /// <summary>
    /// Mild clear weather.
    /// </summary>
    Fair,
    /// <summary>
    /// Cloudy weather.
    /// </summary>
    Cloudy,
    /// <summary>
    /// Light rain.
    /// </summary>
    Rain,
    /// <summary>
    /// Heavy rain.
    /// </summary>
    HardRain,
    /// <summary>
    /// Snow conditions.
    /// </summary>
    Snow,
    /// <summary>
    /// Fog conditions.
    /// </summary>
    Fog
}

/// <summary>
/// Represents a weather forecast or observation for a given period.
/// </summary>
public sealed class WeatherInfo
{
    /// <summary>
    /// Gets or sets the start of the covered period.
    /// </summary>
    public DateTimeOffset DateDebut { get; set; }
    /// <summary>
    /// Gets or sets the end of the covered period.
    /// </summary>
    public DateTimeOffset DateFin { get; set; }
    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string Label { get; set; }
    /// <summary>
    /// Gets or sets the weather kind.
    /// </summary>
    public WeatherInfoKind Kind { get; set; }
    /// <summary>
    /// Gets or sets the measured or forecast temperature.
    /// </summary>
    public int Temperature { get; set; }
    /// <summary>
    /// Gets or sets the perceived temperature.
    /// </summary>
    public int TemperatureFeltAs { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether thunder risk exists.
    /// </summary>
    public bool RiskOfThumber { get; set; }

    /// <summary>
    /// Returns the default display label for a weather kind.
    /// </summary>
    public static string GetLabelFromKind(WeatherInfoKind kind) => kind.ToString();
}

/// <summary>
/// Describes the severity of a weather hazard.
/// </summary>
public enum WeatherHazardSeverity
{
    /// <summary>
    /// Mild severity.
    /// </summary>
    Mild,
    /// <summary>
    /// Moderate severity.
    /// </summary>
    Moderate,
    /// <summary>
    /// Important severity.
    /// </summary>
    Important
}

/// <summary>
/// Identifies a weather hazard category.
/// </summary>
public enum WeatherHazardKind
{
    /// <summary>
    /// Unspecified hazard kind.
    /// </summary>
    Other = 0,
    /// <summary>
    /// Strong wind.
    /// </summary>
    Wind = 1,
    /// <summary>
    /// Heavy rain.
    /// </summary>
    HardRain,
    /// <summary>
    /// Storm conditions.
    /// </summary>
    Storm,
    /// <summary>
    /// Flood risk.
    /// </summary>
    Flood,
    /// <summary>
    /// Snow conditions.
    /// </summary>
    Snow,
    /// <summary>
    /// High temperature event.
    /// </summary>
    HighTemperature,
    /// <summary>
    /// Low temperature event.
    /// </summary>
    LowTemperature
}

/// <summary>
/// Represents a weather hazard affecting the mesh location.
/// </summary>
public sealed class WeatherHazard
{
    /// <summary>
    /// Gets or sets the start of the hazard window.
    /// </summary>
    public DateTimeOffset DateDebut { get; set; }
    /// <summary>
    /// Gets or sets the end of the hazard window.
    /// </summary>
    public DateTimeOffset DateFin { get; set; }
    /// <summary>
    /// Gets or sets the hazard kind.
    /// </summary>
    public WeatherHazardKind Kind { get; set; }
    /// <summary>
    /// Gets or sets the hazard severity.
    /// </summary>
    public WeatherHazardSeverity Severity { get; set; }
    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string Label { get; set; }
}