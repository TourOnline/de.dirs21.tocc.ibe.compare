using System.Collections.Generic;
using System.IO;
using TOCC.IBE.Compare.Models.Common;

namespace TOCC.IBE.Compare.Models.Services
{
    /// <summary>
    /// Interface for generating test cases from test-cases.json file.
    /// </summary>
    public interface ITestCaseGenerator
    {
        /// <summary>
        /// Loads query configurations from JSON content or file path.
        /// </summary>
        List<QueryConfiguration> LoadQueryConfigurations(string jsonContentOrPath = null);
        
        /// <summary>
        /// Loads query configurations from a stream.
        /// </summary>
        List<QueryConfiguration> LoadQueryConfigurationsFromStream(Stream stream);

        /// <summary>
        /// Creates a default test case: today + 1 day, 2 adults, 1 night.
        /// </summary>
        TestCaseWithName CreateDefaultTestCase(string channelUuid);

        /// <summary>
        /// Generates test cases with random dates for all query configurations.
        /// Includes a default test case at the beginning.
        /// </summary>
        List<TestCaseWithName> GenerateTestCases(
            List<QueryConfiguration> configurations,
            string channelUuid,
            int daysFromNowMin = 0,
            int daysFromNowMax = 90,
            bool includeDefaultTestCase = true);
    }
}
