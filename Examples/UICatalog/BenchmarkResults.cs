using System;
using System.Text.Json.Serialization;

namespace UICatalog;

public class BenchmarkResults
{
    [JsonInclude]
    public string Scenario { get; set; }

    [JsonInclude]
    public TimeSpan Duration { get; set; }

    [JsonInclude]
    public int IterationCount { get; set; } = 0;
    [JsonInclude]
    public int ClearedContentCount { get; set; } = 0;
    [JsonInclude]
    public int RefreshedCount { get; set; } = 0;
    [JsonInclude]
    public int UpdatedCount { get; set; } = 0;
    [JsonInclude]
    public int DrawCompleteCount { get; set; } = 0;

    [JsonInclude]
    public int LaidOutCount { get; set; } = 0;
}
