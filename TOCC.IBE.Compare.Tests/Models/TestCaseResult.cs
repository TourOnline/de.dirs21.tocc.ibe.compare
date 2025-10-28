using System.Collections.Generic;
using TOCC.IBE.Compare.Models.Core;

namespace TOCC.IBE.Compare.Tests.Models
{
    /// <summary>
    /// Result of comparing a single test case.
    /// </summary>
    public class TestCaseResult
    {
        public string Oid { get; set; }
        public string Uuid { get; set; }
        public string PropertyDescription { get; set; }
        public string QueryConfigName { get; set; }
        public string From { get; set; }
        public string Until { get; set; }
        public int Los { get; set; }
        public string ChannelUuid { get; set; }
        public bool Success { get; set; }
        public List<Difference> Differences { get; set; }
        public string ErrorMessage { get; set; }
        public string V1ResponseJson { get; set; }
        public string V2ResponseJson { get; set; }
        public long V1ExecutionTimeMs { get; set; }
        public long V2ExecutionTimeMs { get; set; }
    }
}
