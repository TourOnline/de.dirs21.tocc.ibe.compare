using System.Collections.Generic;

namespace TOCC.IBE.Compare.Tests.Models
{
    /// <summary>
    /// DTO for QueryParameters configuration in appsettings.
    /// Uses string types for easier JSON configuration.
    /// </summary>
    public class QueryParametersDto
    {
        public string BuildLevel { get; set; }
        public string From { get; set; }
        public string Until { get; set; }
        public int Los { get; set; }
        public List<string> Occupancy { get; set; }
        public string OutputMode { get; set; }
        public string SessionId { get; set; }
        public List<string> FilterByProducts { get; set; }
        public List<string> FilterByLegacyProducts { get; set; }
        public List<string> FilterByLegacyTariffs { get; set; }
        public List<string> FilterByTariffs { get; set; }
    }
}
