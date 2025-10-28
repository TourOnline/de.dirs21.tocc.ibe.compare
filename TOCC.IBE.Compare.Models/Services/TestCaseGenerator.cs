using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TOCC.IBE.Compare.Models.Common;

namespace TOCC.IBE.Compare.Models.Services
{
    /// <summary>
    /// Generates test cases from test-cases.json file.
    /// Shared between API and test projects for consistency.
    /// </summary>
    public class TestCaseGenerator : ITestCaseGenerator
    {
        /// <summary>
        /// Loads query configurations from JSON content or file path.
        /// </summary>
        public List<QueryConfiguration> LoadQueryConfigurations(string jsonContentOrPath = null)
        {
            string json;

            if (string.IsNullOrEmpty(jsonContentOrPath))
            {
                throw new ArgumentException("JSON content or file path must be provided");
            }

            // Check if it's a file path
            if (File.Exists(jsonContentOrPath))
            {
                json = File.ReadAllText(jsonContentOrPath);
            }
            else
            {
                // Assume it's JSON content
                json = jsonContentOrPath;
            }

            return ParseQueryConfigurations(json);
        }

        /// <summary>
        /// Loads query configurations from a stream (e.g., embedded resource).
        /// </summary>
        public List<QueryConfiguration> LoadQueryConfigurationsFromStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (var reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                return ParseQueryConfigurations(json);
            }
        }

        /// <summary>
        /// Parses query configurations from JSON string.
        /// </summary>
        private List<QueryConfiguration> ParseQueryConfigurations(string json)
        {
            var testData = JsonConvert.DeserializeObject<TestDataDto>(json);

            return testData?.QueryConfigurations?
                .Where(q => !q.Disabled)
                .Select(q => new QueryConfiguration
                {
                    Name = q.Name,
                    Parameters = q.Parameters
                })
                .ToList() ?? new List<QueryConfiguration>();
        }

        /// <summary>
        /// Creates a default test case: today + 1 day, 2 adults, 1 night.
        /// </summary>
        public TestCaseWithName CreateDefaultTestCase(string channelUuid)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return new TestCaseWithName
            {
                Name = "Default: Today+1, 2 Adults",
                Parameters = new TestCaseParameters
                {
                    BuildLevel = "Primary",
                    From = today.ToString("yyyy-MM-dd"),
                    Until = tomorrow.ToString("yyyy-MM-dd"),
                    Los = 1,
                    Occupancy = new List<string> { "a,a" },
                    OutputMode = "Availability",
                    ChannelUuid = channelUuid,
                    SessionId = Guid.NewGuid().ToString(),
                    FilterByProducts = new List<string>(),
                    FilterByLegacyProducts = new List<string>(),
                    FilterByLegacyTariffs = new List<string>(),
                    FilterByTariffs = new List<string>()
                }
            };
        }

        /// <summary>
        /// Generates test cases with random dates for all query configurations.
        /// Includes a default test case at the beginning.
        /// </summary>
        public List<TestCaseWithName> GenerateTestCases(
            List<QueryConfiguration> configurations,
            string channelUuid,
            int daysFromNowMin = 0,
            int daysFromNowMax = 90,
            bool includeDefaultTestCase = true)
        {
            var random = new Random();
            var testCases = new List<TestCaseWithName>();

            // Add default test case first if requested
            if (includeDefaultTestCase)
            {
                testCases.Add(CreateDefaultTestCase(channelUuid));
            }

            foreach (var config in configurations)
            {
                var parameters = config.Parameters;
                
                // Generate random dates based on Los
                var daysFromNow = random.Next(daysFromNowMin, daysFromNowMax);
                var fromDate = DateTime.Today.AddDays(daysFromNow);
                var untilDate = fromDate.AddDays(parameters.Los);

                testCases.Add(new TestCaseWithName
                {
                    Name = config.Name,
                    Parameters = new TestCaseParameters
                    {
                        Occupancy = parameters.Occupancy ?? new List<string> { "a,a" },
                        BuildLevel = parameters.BuildLevel ?? "Primary",
                        From = fromDate.ToString("yyyy-MM-dd"),
                        Until = untilDate.ToString("yyyy-MM-dd"),
                        Los = parameters.Los,
                        UnitUuid = parameters.UnitUuid ?? new List<string>(),
                        OutputMode = parameters.OutputMode ?? "Availability",
                        ChannelUuid = channelUuid,
                        SessionId = parameters.SessionId,
                        FilterByProducts = parameters.FilterByProducts ?? new List<string>(),
                        FilterByLegacyProducts = parameters.FilterByLegacyProducts ?? new List<string>(),
                        FilterByLegacyTariffs = parameters.FilterByLegacyTariffs ?? new List<string>(),
                        FilterByTariffs = parameters.FilterByTariffs ?? new List<string>()
                    }
                });
            }

            return testCases;
        }

        /// <summary>
        /// DTO for loading test data from JSON.
        /// </summary>
        private class TestDataDto
        {
            public List<QueryConfigDto> QueryConfigurations { get; set; } = new();
        }

        /// <summary>
        /// DTO for query configuration from JSON.
        /// </summary>
        private class QueryConfigDto
        {
            public string Name { get; set; } = string.Empty;
            public bool Disabled { get; set; }
            public TestCaseParameters Parameters { get; set; } = new();
        }
    }

    /// <summary>
    /// Query configuration with name.
    /// </summary>
    public class QueryConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public TestCaseParameters Parameters { get; set; } = new();
    }

    /// <summary>
    /// Test case with name and parameters.
    /// </summary>
    public class TestCaseWithName
    {
        public string Name { get; set; } = string.Empty;
        public TestCaseParameters Parameters { get; set; } = new();
    }
}
