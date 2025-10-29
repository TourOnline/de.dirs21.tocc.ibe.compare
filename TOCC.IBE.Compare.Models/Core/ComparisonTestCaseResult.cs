using System.Collections.Generic;
using Newtonsoft.Json;

namespace TOCC.IBE.Compare.Models.Core
{
    /// <summary>
    /// Unified result of a single test case comparison.
    /// Used by both API responses and integration tests.
    /// </summary>
    public class ComparisonTestCaseResult
    {
        [JsonProperty("oid")]
        public string Oid { get; set; } = string.Empty;

        [JsonProperty("uuid")]
        public string Uuid { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("testCaseName")]
        public string? TestCaseName { get; set; }

        [JsonProperty("occupancy")]
        public List<string>? Occupancy { get; set; }

        [JsonProperty("buildLevel")]
        public string? BuildLevel { get; set; }

        [JsonProperty("from")]
        public string From { get; set; } = string.Empty;

        [JsonProperty("until")]
        public string Until { get; set; } = string.Empty;

        [JsonProperty("los")]
        public int Los { get; set; }

        [JsonProperty("channelUuid")]
        public string? ChannelUuid { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("differenceCount")]
        public int DifferenceCount { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("differences")]
        public List<Difference>? Differences { get; set; }

        [JsonProperty("businessFriendlyDifferences")]
        public List<BusinessFriendlyDifference>? BusinessFriendlyDifferences { get; set; }

        [JsonProperty("v1ExecutionTimeMs")]
        public long V1ExecutionTimeMs { get; set; }

        [JsonProperty("v2ExecutionTimeMs")]
        public long V2ExecutionTimeMs { get; set; }

        // Additional properties for integration tests (not serialized to API)
        [JsonIgnore]
        public string? V1ResponseJson { get; set; }

        [JsonIgnore]
        public string? V2ResponseJson { get; set; }
    }
}
