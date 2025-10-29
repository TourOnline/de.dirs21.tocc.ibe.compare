using System;
using System.Collections.Generic;
using System.Linq;
using TOCC.IBE.Compare.Models.Core;

namespace TOCC.IBE.Compare.Models.Services
{
    /// <summary>
    /// Maps technical differences to business-friendly descriptions for non-technical users.
    /// </summary>
    public static class BusinessFriendlyMapper
    {
        private static readonly Dictionary<string, PathMapping> PathMappings = new()
        {
            // Pricing mappings
            { "Price", new PathMapping
            {
                Category = "💰 Pricing",
                FieldName = "Price",
                Severity = DifferenceSeverity.Critical,
                ImpactTemplate = "Price difference detected",
                RecommendationTemplate = "Review pricing strategy - this impacts booking decisions"
            }},
            { "TotalPrice", new PathMapping
            {
                Category = "💰 Pricing",
                FieldName = "Total Price",
                Severity = DifferenceSeverity.Critical,
                ImpactTemplate = "Total booking cost differs between systems",
                RecommendationTemplate = "Critical: Verify pricing calculations immediately"
            }},
            
            // Availability mappings
            { "IsAvailable", new PathMapping
            {
                Category = "🏨 Availability",
                FieldName = "Availability Status",
                Severity = DifferenceSeverity.Critical,
                ImpactTemplate = "Room availability status differs",
                RecommendationTemplate = "Critical: Guests may see incorrect availability"
            }},
            { "RemainingUnits", new PathMapping
            {
                Category = "🏨 Availability",
                FieldName = "Rooms Remaining",
                Severity = DifferenceSeverity.High,
                ImpactTemplate = "Different inventory counts shown",
                RecommendationTemplate = "Review: May cause overbooking or missed revenue"
            }},
            
            // Restrictions mappings
            { "MinStayThrough", new PathMapping
            {
                Category = "📋 Booking Rules",
                FieldName = "Minimum Stay Requirement",
                Severity = DifferenceSeverity.Medium,
                ImpactTemplate = "Minimum stay rules differ",
                RecommendationTemplate = "Review: May affect booking restrictions"
            }},
            { "MinLos", new PathMapping
            {
                Category = "📋 Booking Rules",
                FieldName = "Minimum Length of Stay",
                Severity = DifferenceSeverity.Medium,
                ImpactTemplate = "Min LOS rules differ",
                RecommendationTemplate = "Review: Booking validation may differ"
            }},
            { "IsCta", new PathMapping
            {
                Category = "📋 Booking Rules",
                FieldName = "Closed to Arrival",
                Severity = DifferenceSeverity.High,
                ImpactTemplate = "CTA restriction differs",
                RecommendationTemplate = "Important: Check-in restrictions may block bookings"
            }},
            { "IsCtd", new PathMapping
            {
                Category = "📋 Booking Rules",
                FieldName = "Closed to Departure",
                Severity = DifferenceSeverity.High,
                ImpactTemplate = "CTD restriction differs",
                RecommendationTemplate = "Important: Check-out restrictions may block bookings"
            }},
            
            // Room/Product mappings
            { "RoomType", new PathMapping
            {
                Category = "🛏️ Room Details",
                FieldName = "Room Type",
                Severity = DifferenceSeverity.High,
                ImpactTemplate = "Room type information differs",
                RecommendationTemplate = "Review: Guest expectations may not match"
            }},
            { "Description", new PathMapping
            {
                Category = "🛏️ Room Details",
                FieldName = "Description",
                Severity = DifferenceSeverity.Low,
                ImpactTemplate = "Description text differs",
                RecommendationTemplate = "Info: Content differences detected"
            }},
            
            // Cache mappings
            { "CacheSetName", new PathMapping
            {
                Category = "⚙️ Technical",
                FieldName = "Cache Name",
                Severity = DifferenceSeverity.Info,
                ImpactTemplate = "Technical cache identifier differs",
                RecommendationTemplate = "Info: No user impact - technical difference only"
            }},
            { "IsFromCache", new PathMapping
            {
                Category = "⚙️ Technical",
                FieldName = "Cache Status",
                Severity = DifferenceSeverity.Info,
                ImpactTemplate = "Cache usage differs",
                RecommendationTemplate = "Info: No user impact - performance optimization difference"
            }}
        };

        /// <summary>
        /// Converts technical difference to business-friendly format.
        /// </summary>
        public static BusinessFriendlyDifference MapToBusinessFriendly(Difference difference)
        {
            var pathMapping = FindBestMapping(difference.Path);
            
            return new BusinessFriendlyDifference
            {
                Category = pathMapping.Category,
                FieldName = pathMapping.FieldName,
                Severity = pathMapping.Severity,
                WhatChanged = GenerateWhatChanged(difference, pathMapping),
                BusinessImpact = pathMapping.ImpactTemplate,
                Recommendation = pathMapping.RecommendationTemplate,
                OldValue = FormatValue(difference.ExpectedDisplay),
                NewValue = FormatValue(difference.ActualDisplay),
                Indicator = GetSeverityIndicator(pathMapping.Severity),
                TechnicalPath = difference.Path
            };
        }

        private static PathMapping FindBestMapping(string path)
        {
            // Try exact match first
            var pathParts = path.Split('.');
            foreach (var part in pathParts.Reverse())
            {
                if (PathMappings.TryGetValue(part, out var mapping))
                    return mapping;
            }

            // Default mapping
            return new PathMapping
            {
                Category = "📊 Other",
                FieldName = pathParts.LastOrDefault() ?? "Unknown Field",
                Severity = DifferenceSeverity.Medium,
                ImpactTemplate = "Data difference detected",
                RecommendationTemplate = "Review this difference with technical team"
            };
        }

        private static string GenerateWhatChanged(Difference diff, PathMapping mapping)
        {
            return diff.Type switch
            {
                DifferenceType.ValueMismatch => $"{mapping.FieldName} changed from '{diff.ExpectedDisplay}' to '{diff.ActualDisplay}'",
                DifferenceType.MissingInV1 => $"{mapping.FieldName} is new in V2 (value: '{diff.ActualDisplay}')",
                DifferenceType.MissingInV2 => $"{mapping.FieldName} was removed in V2 (was: '{diff.ExpectedDisplay}')",
                DifferenceType.Count => $"{mapping.FieldName} count changed from {diff.ExpectedDisplay} to {diff.ActualDisplay}",
                _ => $"{mapping.FieldName} differs between V1 and V2"
            };
        }

        private static string FormatValue(string value)
        {
            if (value == "<null>") return "Not Set";
            if (value == "<empty>") return "Empty";
            if (value.StartsWith("[") && value.EndsWith("items]")) return value;
            if (value.StartsWith("<") && value.EndsWith(">")) return "Complex Data";
            return value;
        }

        private static string GetSeverityIndicator(DifferenceSeverity severity)
        {
            return severity switch
            {
                DifferenceSeverity.Critical => "🔴",
                DifferenceSeverity.High => "🟠",
                DifferenceSeverity.Medium => "🟡",
                DifferenceSeverity.Low => "🟢",
                DifferenceSeverity.Info => "ℹ️",
                _ => "⚪"
            };
        }

        private class PathMapping
        {
            public string Category { get; set; } = string.Empty;
            public string FieldName { get; set; } = string.Empty;
            public DifferenceSeverity Severity { get; set; }
            public string ImpactTemplate { get; set; } = string.Empty;
            public string RecommendationTemplate { get; set; } = string.Empty;
        }
    }
}
