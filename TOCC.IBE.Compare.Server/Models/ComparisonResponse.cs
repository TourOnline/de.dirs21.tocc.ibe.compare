using System.Collections.Generic;
using Newtonsoft.Json;
using TOCC.IBE.Compare.Models.Core;

namespace TOCC.IBE.Compare.Server.Models
{
    /// <summary>
    /// Response model for comparison API.
    /// </summary>
    public class ComparisonResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("totalTestCases")]
        public int TotalTestCases { get; set; }

        [JsonProperty("successfulComparisons")]
        public int SuccessfulComparisons { get; set; }

        [JsonProperty("failedComparisons")]
        public int FailedComparisons { get; set; }

        [JsonProperty("errors")]
        public int Errors { get; set; }

        [JsonProperty("results")]
        public List<ComparisonTestCaseResult> Results { get; set; } = new();

        [JsonProperty("summary")]
        public ComparisonSummary Summary { get; set; } = new();
    }

    /// <summary>
    /// Overall comparison summary.
    /// </summary>
    public class ComparisonSummary
    {
        [JsonProperty("totalProperties")]
        public int TotalProperties { get; set; }

        [JsonProperty("totalTestCases")]
        public int TotalTestCases { get; set; }

        [JsonProperty("successRate")]
        public double SuccessRate { get; set; }

        [JsonProperty("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; } = string.Empty;
    }
}
