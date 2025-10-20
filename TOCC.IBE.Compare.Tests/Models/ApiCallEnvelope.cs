using Newtonsoft.Json;
using System.Collections.Generic;
using TOCC.IBE.Compare.Models.Common;

namespace TOCC.IBE.Compare.Tests.Models
{
    /// <summary>
    /// Envelope describing the HTTP call to the API.
    /// </summary>
    public class ApiCallEnvelope
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("headers")]
        public Dictionary<string, string> Headers { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("body")]
        public QueryParameters Body { get; set; }
    }
}
