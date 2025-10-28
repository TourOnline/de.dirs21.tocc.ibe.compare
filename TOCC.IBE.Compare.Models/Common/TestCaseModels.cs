using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace TOCC.IBE.Compare.Models.Common
{
    /// <summary>
    /// Request model for comparison operations.
    /// Shared between API and test projects.
    /// </summary>
    public class ComparisonRequest
    {
        [Required]
        [JsonProperty("Properties")]
        public List<PropertyTestCase> Properties { get; set; } = new();
    }

    /// <summary>
    /// Represents a property with its test cases.
    /// Shared between API and test projects.
    /// </summary>
    public class PropertyTestCase
    {
        [Required]
        [JsonProperty("_oid")]
        public string Oid { get; set; } = string.Empty;

        [Required]
        [JsonProperty("_uuid")]
        public string Uuid { get; set; } = string.Empty;

        [JsonProperty("Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [JsonProperty("UsePreDefinedTestCases")]
        public string UsePreDefinedTestCases { get; set; } = "true";

        [JsonProperty("TestCases")]
        public List<TestCaseParameters>? TestCases { get; set; }
    }

    /// <summary>
    /// Test case parameters for availability query.
    /// DTO using string types for easier JSON configuration.
    /// Shared between API and test projects.
    /// </summary>
    public class TestCaseParameters
    {
        [Required]
        [JsonProperty("Occupancy")]
        public List<string> Occupancy { get; set; } = new();

        [Required]
        [JsonProperty("BuildLevel")]
        public string BuildLevel { get; set; } = "Primary";

        [Required]
        [JsonProperty("From")]
        public string From { get; set; } = string.Empty;

        [Required]
        [JsonProperty("Until")]
        public string Until { get; set; } = string.Empty;

        [Required]
        [JsonProperty("Los")]
        public int Los { get; set; }

        [JsonProperty("UnitUuid")]
        public List<string> UnitUuid { get; set; } = new();

        [Required]
        [JsonProperty("OutputMode")]
        public string OutputMode { get; set; } = "Availability";

        [JsonProperty("Channel_uuid")]
        public string? ChannelUuid { get; set; }

        [JsonProperty("SessionId")]
        public string? SessionId { get; set; }

        [JsonProperty("FilterByProducts")]
        public List<string> FilterByProducts { get; set; } = new();

        [JsonProperty("FilterByLegacyProducts")]
        public List<string> FilterByLegacyProducts { get; set; } = new();

        [JsonProperty("FilterByLegacyTariffs")]
        public List<string> FilterByLegacyTariffs { get; set; } = new();

        [JsonProperty("FilterByTariffs")]
        public List<string> FilterByTariffs { get; set; } = new();
    }

    /// <summary>
    /// Envelope describing the HTTP call to the API.
    /// Shared between API and test projects.
    /// </summary>
    public class ApiCallEnvelope
    {
        [JsonProperty("path")]
        public string Path { get; set; } = string.Empty;

        [JsonProperty("headers")]
        public Dictionary<string, string> Headers { get; set; } = new();

        [JsonProperty("method")]
        public string Method { get; set; } = "POST";

        [JsonProperty("body")]
        public QueryParameters Body { get; set; } = new();
    }
}
