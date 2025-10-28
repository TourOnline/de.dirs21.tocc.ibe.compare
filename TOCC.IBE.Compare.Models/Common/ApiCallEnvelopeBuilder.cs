using System;
using System.Collections.Generic;
using System.Linq;
using TOCC.Core.Models;
using TOCC.IBE.Compare.Models.Common;

namespace TOCC.IBE.Compare.Models.Common
{
    /// <summary>
    /// Shared helper for building API call envelopes from test case parameters.
    /// </summary>
    public class ApiCallEnvelopeBuilder : IApiCallEnvelopeBuilder
    {
        /// <summary>
        /// Builds API call envelope from test case parameters.
        /// </summary>
        public ApiCallEnvelope BuildEnvelope(string channelUuid, TestCaseParameters parameters)
        {
            // Parse dates
            DateTime? fromDate = DateTime.TryParse(parameters.From, out var parsedFrom) ? parsedFrom : (DateTime?)null;
            DateTime? untilDate = DateTime.TryParse(parameters.Until, out var parsedUntil) ? parsedUntil : (DateTime?)null;

            // Parse enums
            Enum.TryParse<BuildLevels>(parameters.BuildLevel, out var buildLevel);
            Enum.TryParse<OutputModes>(parameters.OutputMode, out var outputMode);

            // Parse session ID if provided
            Guid? sessionId = null;
            if (!string.IsNullOrEmpty(parameters.SessionId) && Guid.TryParse(parameters.SessionId, out var parsedSessionId))
            {
                sessionId = parsedSessionId;
            }

            // Build query parameters
            var queryParams = new QueryParameters
            {
                BuildLevel = buildLevel,
                From = fromDate,
                Until = untilDate,
                Los = parameters.Los,
                Occupancy = parameters.Occupancy?.ToArray() ?? Array.Empty<string>(),
                OutputMode = outputMode,
                Channel_uuid = Guid.Parse(channelUuid),
                SessionId = sessionId,
                FilterByProducts = ParseGuidList(parameters.FilterByProducts),
                FilterByLegacyProducts = parameters.FilterByLegacyProducts ?? new List<string>(),
                FilterByLegacyTariffs = parameters.FilterByLegacyTariffs ?? new List<string>(),
                FilterByTariffs = ParseGuidList(parameters.FilterByTariffs)
            };

            return new ApiCallEnvelope
            {
                Path = $"ibe/availability/{channelUuid}",
                Headers = new Dictionary<string, string>
                {
                    { "Accept-Language", "en-US" }
                },
                Method = "POST",
                Body = queryParams
            };
        }

        private static List<Guid> ParseGuidList(List<string>? guids)
        {
            if (guids == null || guids.Count == 0)
                return new List<Guid>();

            return guids
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();
        }
    }
}
