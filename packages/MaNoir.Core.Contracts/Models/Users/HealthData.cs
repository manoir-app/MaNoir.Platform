using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Users;

public sealed class HealthData
{
    public HealthData()
    {
        WeightDatas = [];
    }

    public List<WeightData> WeightDatas { get; set; }
}

public sealed class WeightData
{
    public decimal Value { get; set; }
    public DateTimeOffset Date { get; set; }
}