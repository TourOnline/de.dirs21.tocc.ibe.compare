using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TOCC.Core.Models;
using TOCC.IBE.Compare.Models.Common;
using TOCC.IBE.Compare.Models.Core;
using TOCC.IBE.Compare.Models.V1;
using TOCC.IBE.Compare.Tests.Models;
using Xunit;

namespace TOCC.IBE.Compare.Tests.IntegrationTests
{
    /// <summary>
    /// Integration tests for comparing IBE availability API responses between V1 and V2.
    /// These tests can be toggled on/off via environment variable or appsettings.
    /// </summary>
    public class AvailabilityIntegrationTests : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly bool _isEnabled;
        private readonly string _v1BaseUrl;
        private readonly string _v2BaseUrl;
        private readonly string _testCasesFile;
        private readonly string _artifactsFolder;

        public AvailabilityIntegrationTests()
        {
            // Build configuration from appsettings
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                .AddEnvironmentVariables();

            _configuration = builder.Build();

            // Read configuration
            _isEnabled = _configuration.GetValue<bool>("IntegrationTest:Enabled");
            _v1BaseUrl = _configuration["IntegrationTest:V1BaseUrl"];
            _v2BaseUrl = _configuration["IntegrationTest:V2BaseUrl"];
            _testCasesFile = _configuration["IntegrationTest:TestCasesFile"];
            _artifactsFolder = _configuration["IntegrationTest:ArtifactsFolder"] ?? "artifacts";

            // Initialize HTTP client
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
        }

        [Fact]
        public async Task CompareAvailabilityResponses_ForAllTestCases_GeneratesArtifact()
        {
            // Skip if integration tests are disabled
            if (!_isEnabled)
            {
                Console.WriteLine("⚠️ Integration tests are disabled. Set IntegrationTest:Enabled=true in appsettings or environment variable.");
                return;
            }

            // Validate configuration
            Assert.False(string.IsNullOrEmpty(_v1BaseUrl), "V1BaseUrl must be configured");
            Assert.False(string.IsNullOrEmpty(_v2BaseUrl), "V2BaseUrl must be configured");

            Console.WriteLine($"🚀 Starting integration tests");
            Console.WriteLine($"   V1 Base URL: {_v1BaseUrl}");
            Console.WriteLine($"   V2 Base URL: {_v2BaseUrl}");
            Console.WriteLine();

            // Load test cases
            var testCases = LoadTestCases();
            Console.WriteLine($"📋 Loaded {testCases.Count} test cases");

            // Process each test case
            var results = new List<TestCaseResult>();
            int currentCase = 0;

            foreach (var testCase in testCases)
            {
                currentCase++;
                Console.WriteLine($"\n[{currentCase}/{testCases.Count}] Property: {testCase.PropertyDescription} (OID={testCase.Oid})");
                Console.WriteLine($"   Query: {testCase.QueryConfigName}");

                var result = await ProcessTestCase(testCase);
                results.Add(result);

                if (result.Success)
                {
                    Console.WriteLine($"   ✅ No differences found");
                }
                else if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.WriteLine($"   ❌ Error: {result.ErrorMessage}");
                }
                else
                {
                    Console.WriteLine($"   ⚠️ Found {result.Differences.Count} differences");
                }
            }

            // Generate artifacts
            var summaryPath = GenerateArtifact(results);
            Console.WriteLine($"\n📦 Generated {results.Count} individual artifact files");
            Console.WriteLine($"📦 Summary artifact: {summaryPath}");

            // Publish TeamCity artifact
            PublishTeamCityArtifact(summaryPath);

            // Summary
            var successCount = results.Count(r => r.Success);
            var errorCount = results.Count(r => !string.IsNullOrEmpty(r.ErrorMessage));
            var diffCount = results.Count(r => !r.Success && string.IsNullOrEmpty(r.ErrorMessage));

            Console.WriteLine($"\n=== Summary ===");
            Console.WriteLine($"Total test cases: {results.Count}");
            Console.WriteLine($"✅ Identical: {successCount}");
            Console.WriteLine($"⚠️ With differences: {diffCount}");
            Console.WriteLine($"❌ Errors: {errorCount}");

            // Assert: Test passes only if all responses are identical (no differences, no errors)
            Assert.True(errorCount == 0, $"Test failed: {errorCount} test case(s) had errors. Check artifacts folder for details: {Path.GetDirectoryName(summaryPath)}");
            Assert.True(diffCount == 0, $"Test failed: {diffCount} test case(s) have differences between V1 and V2. Check artifacts folder for details: {Path.GetDirectoryName(summaryPath)}");
            Assert.True(successCount == results.Count, $"Test failed: Not all test cases passed. Only {successCount}/{results.Count} were identical.");
            
            Console.WriteLine($"\n✅ All {results.Count} test cases passed - V1 and V2 responses are identical!");
        }

        private List<TestCase> LoadTestCases()
        {
            var baseDir = AppContext.BaseDirectory;
            var testCasesPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", _testCasesFile));

            if (!File.Exists(testCasesPath))
            {
                throw new FileNotFoundException($"Test cases file not found: {testCasesPath}");
            }

            var json = File.ReadAllText(testCasesPath);
            var testData = JsonConvert.DeserializeObject<TestData>(json);

            // Generate cartesian product: n properties × m query configurations = n*m test cases
            var testCases = new List<TestCase>();

            foreach (var property in testData.Properties)
            {
                // Always add default test case: today + 1 day, 2 adults
                testCases.Add(CreateDefaultTestCase(property));

                // Add configured test cases
                foreach (var queryConfig in testData.QueryConfigurations)
                {
                    var testCase = new TestCase
                    {
                        Oid = property._oid,
                        Uuid = property._uuid,
                        PropertyDescription = property.Description ?? property._oid,
                        QueryConfigName = queryConfig.Name,
                        QueryParameters = PopulateDatesIfNeeded(queryConfig.Parameters)
                    };
                    
                    testCases.Add(testCase);
                }
            }

            return testCases;
        }

        private TestCase CreateDefaultTestCase(PropertyInfo property)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return new TestCase
            {
                Oid = property._oid,
                Uuid = property._uuid,
                PropertyDescription = property.Description ?? property._oid,
                QueryConfigName = "Default: Today+1, 2 Adults",
                QueryParameters = new QueryParametersDto
                {
                    BuildLevel = "Primary",
                    From = today.ToString("yyyy-MM-dd"),
                    Until = tomorrow.ToString("yyyy-MM-dd"),
                    Los = 1,
                    Occupancy = new List<string> { "a,a" },
                    OutputMode = "Availability",
                    SessionId = Guid.NewGuid().ToString(),
                    FilterByProducts = new List<string>(),
                    FilterByLegacyProducts = new List<string>(),
                    FilterByLegacyTariffs = new List<string>(),
                    FilterByTariffs = new List<string>()
                }
            };
        }

        private QueryParametersDto PopulateDatesIfNeeded(QueryParametersDto parameters)
        {
            // If From/Until are not provided but Los is, generate random dates
            if ((string.IsNullOrEmpty(parameters.From) || string.IsNullOrEmpty(parameters.Until)) && parameters.Los > 0)
            {
                var random = new Random();
                var daysFromNow = random.Next(0, 90); // Random date within next 90 days
                var fromDate = DateTime.Today.AddDays(daysFromNow);
                var untilDate = fromDate.AddDays(parameters.Los);

                parameters.From = fromDate.ToString("yyyy-MM-dd");
                parameters.Until = untilDate.ToString("yyyy-MM-dd");
            }

            return parameters;
        }

        private async Task<TestCaseResult> ProcessTestCase(TestCase testCase)
        {
            var result = new TestCaseResult
            {
                Oid = testCase.Oid,
                Uuid = testCase.Uuid,
                PropertyDescription = testCase.PropertyDescription,
                QueryConfigName = testCase.QueryConfigName,
                From = testCase.QueryParameters?.From,
                Until = testCase.QueryParameters?.Until,
                Los = testCase.QueryParameters?.Los ?? 0,
                ChannelUuid = testCase.Uuid,
                Differences = new List<Compare.Models.Core.Difference>()
            };

            try
            {
                // Build API call envelope
                var envelope = BuildApiCallEnvelope(testCase);

                // Call V1 API
                var v1Response = await CallApi(_v1BaseUrl, envelope);
                result.V1ResponseJson = v1Response;
                var v1Data = JsonConvert.DeserializeObject<ApiResult<V1Response>>(v1Response);

                // Call V2 API
                var v2Response = await CallApi(_v2BaseUrl, envelope);
                result.V2ResponseJson = v2Response;
                var v2Data = JsonConvert.DeserializeObject<ApiResult<TOCC.Contracts.IBE.Models.Availability.Response>>(v2Response);

                //v2Data.Value.Result.Properties.First().Periods.First().Sets.First().Products.First().Ticks.First().Offers.First().Ticks.First().PersonPrices;
                // Compare responses
                var comparer = new AvailabilityComparer();
                ConfigureComparer(comparer);

                bool areEqual = comparer.Compare(v1Data.Value, v2Data.Value);
                result.Success = areEqual;
                result.Differences = comparer.Differences.ToList();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
            }

            return result;
        }

        private ApiCallEnvelope BuildApiCallEnvelope(TestCase testCase)
        {
            // Use query parameters from test case
            var queryParamsDto = testCase.QueryParameters;

            // Parse dates
            DateTime? fromDate = null;
            DateTime? untilDate = null;
            
            if (queryParamsDto.From != null && DateTime.TryParse(queryParamsDto.From, out var parsedFrom))
                fromDate = parsedFrom;
            
            if (queryParamsDto.Until != null && DateTime.TryParse(queryParamsDto.Until, out var parsedUntil))
                untilDate = parsedUntil;

            // Parse UUID from test case
            Guid channelUuid = Guid.Parse(testCase.Uuid);
            Guid? sessionId = null;
            if (queryParamsDto.SessionId != null && Guid.TryParse(queryParamsDto.SessionId, out var parsedSessionId))
                sessionId = parsedSessionId;

            // Parse enums
            Enum.TryParse<BuildLevels>(queryParamsDto.BuildLevel, out var buildLevel);
            Enum.TryParse<OutputModes>(queryParamsDto.OutputMode, out var outputMode);

            // Build query parameters using the existing QueryParameters class
            var queryParams = new QueryParameters
            {
                BuildLevel = buildLevel,
                From = fromDate,
                Until = untilDate,
                Los = queryParamsDto.Los,
                Occupancy = queryParamsDto.Occupancy?.ToArray() ?? new string[0],
                OutputMode = outputMode,
                Channel_uuid = channelUuid,
                SessionId = sessionId,
                FilterByProducts = queryParamsDto.FilterByProducts?.Select(Guid.Parse).ToList() ?? new List<Guid>(),
                FilterByLegacyProducts = queryParamsDto.FilterByLegacyProducts ?? new List<string>(),
                FilterByLegacyTariffs = queryParamsDto.FilterByLegacyTariffs ?? new List<string>(),
                FilterByTariffs = queryParamsDto.FilterByTariffs?.Select(Guid.Parse).ToList() ?? new List<Guid>()
            };

            return new ApiCallEnvelope
            {
                Path = $"ibe/availability/{testCase.Uuid}",
                Headers = new Dictionary<string, string>
                {
                    { "Accept-Language", "en-US" }
                },
                Method = "POST",
                Body = queryParams
            };
        }

        private async Task<string> CallApi(string baseUrl, ApiCallEnvelope envelope)
        {
            var url = $"{baseUrl.TrimEnd('/')}/api/__api_party/ibe";
            
            // Configure JSON serializer to convert enums to strings
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() },
                NullValueHandling = NullValueHandling.Ignore
            };
            
            var jsonBody = JsonConvert.SerializeObject(envelope, settings);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Add headers
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            foreach (var header in envelope.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        private void ConfigureComparer(AvailabilityComparer comparer)
        {
            // Configure paths to skip during comparison
            // Note: Array indices and identifiers like [_oid=97936] are automatically removed during comparison
            comparer.SkipPaths = new List<string>
            {
                // Skip specific Ticks properties under Offers
                "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.MinStayThrough",
                "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.MinLos",
                "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.IsCta",
                "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.IsCtd",
                "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.IsMixable",
                "Result.Properties.Periods.Sets.Products.Ticks.InnerTicks",
                // Skip cache-related properties
                "Result.Properties.Periods.CacheSetName",
                "Result.Properties.Periods.IsFromCache",
            };
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Replace(" ", "_").Replace(":", "").Replace("+", "plus");
        }

        private string GenerateArtifact(List<TestCaseResult> results)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var baseDir = AppContext.BaseDirectory;
            var artifactsDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", _artifactsFolder));
            Directory.CreateDirectory(artifactsDir);

            // Generate individual artifact files for each test case
            foreach (var result in results)
            {
                var sanitizedTestName = SanitizeFileName(result.QueryConfigName);
                var individualFileName = $"{timestamp}_oid{result.Oid}_uuid{result.Uuid.Substring(0, 8)}_{sanitizedTestName}.json";
                var individualFilePath = Path.Combine(artifactsDir, individualFileName);

                var individualArtifact = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Oid = result.Oid,
                    Uuid = result.Uuid,
                    Property = result.PropertyDescription,
                    QueryConfiguration = result.QueryConfigName,
                    QueryParameters = new
                    {
                        result.From,
                        result.Until,
                        result.Los,
                        ChannelUuid = result.ChannelUuid
                    },
                    V1BaseUrl = _v1BaseUrl,
                    V2BaseUrl = _v2BaseUrl,
                    result.Success,
                    result.ErrorMessage,
                    DifferenceCount = result.Differences?.Count ?? 0,
                    Differences = result.Differences?.Select(d => new
                    {
                        Type = d.Type.ToString(),
                        d.Path,
                        Expected = d.Expected?.ToString(),
                        Actual = d.Actual?.ToString()
                    }).ToList(),
                    V1Response = result.V1ResponseJson != null ? JsonConvert.DeserializeObject(result.V1ResponseJson) : null,
                    V2Response = result.V2ResponseJson != null ? JsonConvert.DeserializeObject(result.V2ResponseJson) : null
                };

                var individualJson = JsonConvert.SerializeObject(individualArtifact, Formatting.Indented);
                File.WriteAllText(individualFilePath, individualJson);
            }

            // Generate summary artifact
            var artifactPath = Path.Combine(artifactsDir, $"integration-test-summary_{timestamp}.json");

            // Create detailed artifact
            var artifact = new
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                V1BaseUrl = _v1BaseUrl,
                V2BaseUrl = _v2BaseUrl,
                TotalTestCases = results.Count,
                Summary = new
                {
                    Identical = results.Count(r => r.Success),
                    WithDifferences = results.Count(r => !r.Success && string.IsNullOrEmpty(r.ErrorMessage)),
                    Errors = results.Count(r => !string.IsNullOrEmpty(r.ErrorMessage)),
                    TotalDifferences = results.Sum(r => r.Differences?.Count ?? 0)
                },
                Results = results.Select(r => new
                {
                    r.Oid,
                    r.Uuid,
                    Property = r.PropertyDescription,
                    QueryConfiguration = r.QueryConfigName,
                    QueryParameters = new
                    {
                        r.From,
                        r.Until,
                        r.Los,
                        ChannelUuid = r.ChannelUuid
                    },
                    r.Success,
                    r.ErrorMessage,
                    DifferenceCount = r.Differences?.Count ?? 0,
                    Differences = r.Differences?.Select(d => new
                    {
                        Type = d.Type.ToString(),
                        d.Path,
                        Expected = d.Expected?.ToString(),
                        Actual = d.Actual?.ToString()
                    }).ToList()
                }).ToList()
            };

            var json = JsonConvert.SerializeObject(artifact, Formatting.Indented);
            File.WriteAllText(artifactPath, json);

            return artifactPath;
        }

        private void PublishTeamCityArtifact(string artifactPath)
        {
            // Check if running in TeamCity
            var isTeamCity = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));

            if (isTeamCity)
            {
                // TeamCity service message to publish artifact
                // Format: ##teamcity[publishArtifacts 'path']
                Console.WriteLine($"##teamcity[publishArtifacts '{artifactPath}']");
                Console.WriteLine($"📤 Published artifact to TeamCity: {Path.GetFileName(artifactPath)}");
            }
            else
            {
                Console.WriteLine($"ℹ️ Not running in TeamCity - artifact saved locally only");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
