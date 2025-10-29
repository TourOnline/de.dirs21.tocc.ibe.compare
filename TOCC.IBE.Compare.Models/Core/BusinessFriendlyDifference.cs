using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TOCC.IBE.Compare.Models.Core
{
    /// <summary>
    /// Business-friendly representation of a difference for non-technical users (POs, stakeholders).
    /// </summary>
    public class BusinessFriendlyDifference
    {
        /// <summary>
        /// Business-friendly category (e.g., "Pricing", "Availability", "Room Details").
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable field name (e.g., "Minimum Stay Through", "Price Per Night").
        /// </summary>
        [JsonProperty("fieldName")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Severity of the difference for business impact.
        /// </summary>
        [JsonProperty("severity")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DifferenceSeverity Severity { get; set; }

        /// <summary>
        /// What changed in plain English.
        /// </summary>
        [JsonProperty("whatChanged")]
        public string WhatChanged { get; set; } = string.Empty;

        /// <summary>
        /// Business impact explanation.
        /// </summary>
        [JsonProperty("businessImpact")]
        public string BusinessImpact { get; set; } = string.Empty;

        /// <summary>
        /// Recommended action for stakeholders.
        /// </summary>
        [JsonProperty("recommendation")]
        public string Recommendation { get; set; } = string.Empty;

        /// <summary>
        /// Old value (V1) in user-friendly format.
        /// </summary>
        [JsonProperty("oldValue")]
        public string OldValue { get; set; } = string.Empty;

        /// <summary>
        /// New value (V2) in user-friendly format.
        /// </summary>
        [JsonProperty("newValue")]
        public string NewValue { get; set; } = string.Empty;

        /// <summary>
        /// Visual indicator for UI (icon, emoji, color).
        /// </summary>
        [JsonProperty("indicator")]
        public string Indicator { get; set; } = string.Empty;

        /// <summary>
        /// Technical path (for developers who need details).
        /// </summary>
        [JsonProperty("technicalPath")]
        public string TechnicalPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Severity level for business impact assessment.
    /// </summary>
    public enum DifferenceSeverity
    {
        /// <summary>
        /// Low impact - informational only.
        /// </summary>
        Low,

        /// <summary>
        /// Medium impact - should be reviewed.
        /// </summary>
        Medium,

        /// <summary>
        /// High impact - needs attention.
        /// </summary>
        High,

        /// <summary>
        /// Critical impact - requires immediate action.
        /// </summary>
        Critical,

        /// <summary>
        /// Informational - no action needed.
        /// </summary>
        Info
    }
}
