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
using TOCC.IBE.Compare.Models.Services;
using TOCC.IBE.Compare.Tests.Models;
using TOCC.IBE.Compare.Tests.Helpers;
using ComparisonTestCaseResult = TOCC.IBE.Compare.Models.Core.ComparisonTestCaseResult;
using TOCC.IBE.Compare.Server.Helpers;
using TOCC.IBE.Compare.Server.Services;
using Xunit;
using ApiCallEnvelope = TOCC.IBE.Compare.Models.Common.ApiCallEnvelope;
using TestCaseParameters = TOCC.IBE.Compare.Models.Common.TestCaseParameters;
using QueryParametersDto = TOCC.IBE.Compare.Models.Common.TestCaseParameters;
using PropertyTestCase = TOCC.IBE.Compare.Models.Common.PropertyTestCase;
using ComparisonRequest = TOCC.IBE.Compare.Models.Common.ComparisonRequest;

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
        private readonly ITestCaseGenerator _testCaseGenerator;
        private readonly IComparisonService _comparisonService;

        public AvailabilityIntegrationTests()
        {
            // Build configuration using centralized ConfigurationHelper
            // Priority: 1. Explicit path, 2. TOCC_CONFIG_PATH env var, 3. Current directory
            _configuration = ConfigurationHelper.BuildConfiguration();

            // Read configuration
            _isEnabled = _configuration.GetValue<bool>("IntegrationTest:Enabled");
            _v1BaseUrl = _configuration["IntegrationTest:V1BaseUrl"];
            _v2BaseUrl = _configuration["IntegrationTest:V2BaseUrl"];
            _testCasesFile = _configuration["IntegrationTest:TestCasesFile"] ?? "TestData/test-cases.json";
            _artifactsFolder = _configuration["IntegrationTest:ArtifactsFolder"] ?? "artifacts";

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };

            // Initialize services (using real implementations to test the actual service)
            _testCaseGenerator = new TestCaseGenerator();
            var envelopeBuilder = new ApiCallEnvelopeBuilder();
            var httpClientFactory = new TestHttpClientFactory(_httpClient);
            var logger = new TestLogger<ComparisonService>();
            
            // Create ComparisonService with all dependencies
            _comparisonService = new ComparisonService(
                _configuration,
                logger,
                httpClientFactory,
                _testCaseGenerator,
                envelopeBuilder);
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

            // Load test data and build comparison request
            var request = BuildComparisonRequest();
            Console.WriteLine($"📋 Loaded {request.Properties.Count} properties with test cases");

            // Execute comparison using ComparisonService with explanations enabled
            // (reuses all the service logic including business-friendly mapping!)
            var response = await _comparisonService.ExecuteComparisonAsync(request, includeExplanations: true);
            var results = response.Results;

            // Display progress
            int currentCase = 0;
            foreach (var result in results)
            {
                currentCase++;
                Console.WriteLine($"\n[{currentCase}/{results.Count}] Property: {result.Description} (OID={result.Oid})");
                Console.WriteLine($"   Query: {result.TestCaseName}");

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
                    Console.WriteLine($"   ⚠️ Found {result.Differences?.Count ?? 0} differences");
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

        private ComparisonRequest BuildComparisonRequest()
        {
            var baseDir = AppContext.BaseDirectory;
            var testCasesPath = Path.Combine(baseDir, _testCasesFile);

            // Load properties from test-cases.json
            var json = File.ReadAllText(testCasesPath);
            var testData = JsonConvert.DeserializeObject<TestData>(json);

            // Build request with properties from test data
            var request = new ComparisonRequest
            {
                Properties = testData.Properties.Select(p => new PropertyTestCase
                {
                    Oid = p._oid,
                    Uuid = p._uuid,
                    Description = p.Description ?? p._oid,
                    UsePreDefinedTestCases = "true" // Use pre-defined test cases from test-cases.json
                }).ToList()
            };

            return request;
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Replace(" ", "_").Replace(":", "").Replace("+", "plus");
        }

        private string GenerateArtifact(List<ComparisonTestCaseResult> results)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var baseDir = AppContext.BaseDirectory;
            var artifactsDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", _artifactsFolder));
            
            // Clear artifacts folder if it exists
            if (Directory.Exists(artifactsDir))
            {
                Directory.Delete(artifactsDir, recursive: true);
            }
            
            Directory.CreateDirectory(artifactsDir);

            // Generate individual artifact files for each test case
            foreach (var result in results)
            {
                var sanitizedTestName = SanitizeFileName(result.TestCaseName);
                var individualFileName = $"{timestamp}_oid{result.Oid}_uuid{result.Uuid.Substring(0, 8)}_{sanitizedTestName}.json";
                var individualFilePath = Path.Combine(artifactsDir, individualFileName);

                var individualArtifact = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Oid = result.Oid,
                    Uuid = result.Uuid,
                    Property = result.Description,
                    QueryConfiguration = result.TestCaseName,
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
                    Property = r.Description,
                    QueryConfiguration = r.TestCaseName,
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
                // TeamCity service message to publish entire artifacts directory
                // Format: ##teamcity[publishArtifacts 'path']
                var artifactsDir = Path.GetDirectoryName(artifactPath);
                Console.WriteLine($"##teamcity[publishArtifacts '{artifactsDir}/**']");
                Console.WriteLine($"📤 Published all artifacts to TeamCity from: {artifactsDir}");
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
