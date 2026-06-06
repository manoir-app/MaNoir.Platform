using System;
using System.Collections.Generic;

namespace MaNoir.Core.Observability;

public sealed class LokiLogEntry
{
    public DateTimeOffset TimestampUtc { get; set; }

    public string Message { get; set; }

    public Dictionary<string, string> Labels { get; set; } = [];
}

public sealed class LokiLogQueryResponse
{
    public string Query { get; set; }

    public string ServiceName { get; set; }

    public string Contains { get; set; }

    public DateTimeOffset StartUtc { get; set; }

    public DateTimeOffset EndUtc { get; set; }

    public string Direction { get; set; }

    public int Limit { get; set; }

    public List<LokiLogEntry> Entries { get; set; } = [];
}