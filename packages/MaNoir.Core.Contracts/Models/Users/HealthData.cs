using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Users;

/// <summary>
/// Represents health-related data tracked for a user.
/// </summary>
public sealed class HealthData
{
    public HealthData()
    {
        WeightDatas = [];
    }

    /// <summary>
    /// Gets or sets the weight history entries.
    /// </summary>
    public List<WeightData> WeightDatas { get; set; }
}

/// <summary>
/// Represents a weight measurement captured for a user.
/// </summary>
public sealed class WeightData
{
    /// <summary>
    /// Gets or sets the measured weight value.
    /// </summary>
    public decimal Value { get; set; }
    /// <summary>
    /// Gets or sets the measurement timestamp.
    /// </summary>
    public DateTimeOffset Date { get; set; }
}