using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TOCC.Core.Models;
using TOCC.IBE.Compare.Models.Common;
using TOCC.IBE.Compare.Models.Core;
using TOCC.IBE.Compare.Models.V1;
using TOCC.IBE.Compare.Models.Services;
using static TOCC.IBE.Compare.Models.Services.BusinessFriendlyMapper;
using TOCC.IBE.Compare.Server.Models;
using TOCC.IBE.Compare.Server.Infrastructure;
using ComparisonRequest = TOCC.IBE.Compare.Models.Common.ComparisonRequest;
using PropertyTestCase = TOCC.IBE.Compare.Models.Common.PropertyTestCase;
using TestCaseParameters = TOCC.IBE.Compare.Models.Common.TestCaseParameters;
using ApiCallEnvelope = TOCC.IBE.Compare.Models.Common.ApiCallEnvelope;

namespace TOCC.IBE.Compare.Server.Services
{
    /// <summary>
    /// Service for comparing V1 and V2 API responses.
    /// Production-ready, cross-platform compatible.
    /// </summary>
    public class ComparisonService : IComparisonService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ComparisonService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ITestCaseGenerator _testCaseGenerator;
        private readonly IApiCallEnvelopeBuilder _envelopeBuilder;
        private readonly string _v1BaseUrl;
        private readonly string _v2BaseUrl;
        private readonly string _testCasesFile;
        private List<QueryConfiguration>? _queryConfigurations;

        public ComparisonService(
            IConfiguration configuration,
            ILogger<ComparisonService> logger,
            IHttpClientFactory httpClientFactory,
            ITestCaseGenerator testCaseGenerator,
            IApiCallEnvelopeBuilder envelopeBuilder)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _testCaseGenerator = testCaseGenerator ?? throw new ArgumentNullException(nameof(testCaseGenerator));
            _envelopeBuilder = envelopeBuilder ?? throw new ArgumentNullException(nameof(envelopeBuilder));
            
            _httpClient = httpClientFactory?.CreateClient() ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _httpClient.Timeout = TimeSpan.FromMinutes(5);

            _v1BaseUrl = _configuration["IntegrationTest:V1BaseUrl"] 
                ?? throw new InvalidOperationException("IntegrationTest:V1BaseUrl not configured");
            _v2BaseUrl = _configuration["IntegrationTest:V2BaseUrl"] 
                ?? throw new InvalidOperationException("IntegrationTest:V2BaseUrl not configured");
            _testCasesFile = _configuration["IntegrationTest:TestCasesFile"] ?? "TestData/test-cases.json";
        }

        /// <summary>
        /// Executes comparison for the given request.
        /// </summary>
        public async Task<ComparisonResponse> ExecuteComparisonAsync(ComparisonRequest request, bool includeExplanations = false)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = new ComparisonResponse
            {
                Success = true,
                Message = "Comparison completed",
                Summary = new ComparisonSummary
                {
                    TotalProperties = request.Properties.Count,
                    Timestamp = DateTime.UtcNow.ToString("O")
                }
            };

            try
            {
                _logger.LogInformation("Starting comparison for {PropertyCount} properties", request.Properties.Count);

                foreach (var property in request.Properties)
                {
                    var testCases = GetTestCasesForProperty(property);
                    response.TotalTestCases += testCases.Count;

                    for (int i = 0; i < testCases.Count; i++)
                    {
                        var testCase = testCases[i];
                        _logger.LogInformation("Processing property {Oid} - test case {Name} ({Index}/{Total})", 
                            property.Oid, testCase.Name, i + 1, testCases.Count);

                        var result = await CompareTestCaseAsync(property, testCase, includeExplanations);
                        response.Results.Add(result);

                        if (result.Success)
                        {
                            response.SuccessfulComparisons++;
                        }
                        else if (!string.IsNullOrEmpty(result.ErrorMessage))
                        {
                            response.Errors++;
                        }
                        else
                        {
                            response.FailedComparisons++;
                        }
                    }
                }

                stopwatch.Stop();
                response.Summary.TotalTestCases = response.TotalTestCases;
                response.Summary.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                response.Summary.SuccessRate = response.TotalTestCases > 0
                    ? (double)response.SuccessfulComparisons / response.TotalTestCases * 100
                    : 0;

                _logger.LogInformation("Comparison completed: {Success}/{Total} successful, {Failed} failed, {Errors} errors",
                    response.SuccessfulComparisons, response.TotalTestCases, response.FailedComparisons, response.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during comparison execution");
                response.Success = false;
                response.Message = $"Comparison failed: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Gets test cases for a property based on UsePreDefinedTestCases flag.
        /// Uses shared TestCaseGenerator for consistency with integration tests.
        /// </summary>
        private List<TestCaseWithName> GetTestCasesForProperty(PropertyTestCase property)
        {
            bool usePreDefined = string.Equals(property.UsePreDefinedTestCases, "true", StringComparison.OrdinalIgnoreCase);

            if (!usePreDefined)
            {
                // Use provided test cases - wrap them with generated names
                var providedCases = property.TestCases ?? new List<TestCaseParameters>();
                return providedCases.Select((tc, index) => new TestCaseWithName
                {
                    Name = $"Custom Test Case {index + 1}",
                    Parameters = tc
                }).ToList();
            }

            // Load query configurations from test-cases.json
            if (_queryConfigurations == null)
            {
                LoadQueryConfigurations();
            }

            // If no configurations loaded, fall back to simple random generation
            if (_queryConfigurations == null || _queryConfigurations.Count == 0)
            {
                _logger.LogWarning("No query configurations found in {TestCasesFile}, using fallback random generation", _testCasesFile);
                return GenerateFallbackTestCase(property);
            }

            // Use shared TestCaseGenerator - same logic as AvailabilityIntegrationTests
            // includeDefaultTestCase: true ensures we always test the default case (today+1, 2 adults)
            return _testCaseGenerator.GenerateTestCases(_queryConfigurations, property.Uuid, 
                daysFromNowMin: 0, daysFromNowMax: 90, includeDefaultTestCase: true);
        }

        /// <summary>
        /// Loads query configurations from test-cases.json file.
        /// No reflection - just simple file loading!
        /// </summary>
        private void LoadQueryConfigurations()
        {
            try
            {
                // File is copied to output directory - just load it directly
                var baseDir = AppContext.BaseDirectory;
                var testCasesPath = Path.Combine(baseDir, _testCasesFile);

                if (File.Exists(testCasesPath))
                {
                    _queryConfigurations = _testCaseGenerator.LoadQueryConfigurations(testCasesPath);
                    _logger.LogInformation("Loaded {Count} query configurations from {File}", 
                        _queryConfigurations.Count, testCasesPath);
                }
                else
                {
                    _logger.LogWarning("Test cases file not found at {Path}", testCasesPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading query configurations from {File}", _testCasesFile);
            }
        }

        /// <summary>
        /// Generates a fallback test case when query configurations are not available.
        /// </summary>
        private List<TestCaseWithName> GenerateFallbackTestCase(PropertyTestCase property)
        {
            var random = new Random();
            var tomorrow = DateTime.UtcNow.AddDays(1);
            var from = tomorrow.AddDays(random.Next(0, 30));
            var los = random.Next(1, 8);
            var until = from.AddDays(los);

            var occupancies = new List<List<string>>
            {
                new List<string> { "a,a" },                    // 2 adults
                new List<string> { "a,a,5" },                  // 2 adults + 1 child (5 years)
                new List<string> { "a,a,10,8" },               // 2 adults + 2 children
                new List<string> { "a,a", "a,a" },             // 2 rooms, 2 adults each
                new List<string> { "a,a,5", "a,a" }            // 2 rooms, mixed
            };

            var selectedOccupancy = occupancies[random.Next(occupancies.Count)];

            return new List<TestCaseWithName>
            {
                new TestCaseWithName
                {
                    Name = "Fallback Random Test Case",
                    Parameters = new TestCaseParameters
                    {
                        Occupancy = selectedOccupancy,
                        BuildLevel = "Primary",
                        From = from.ToString("yyyy-MM-dd"),
                        Until = until.ToString("yyyy-MM-dd"),
                        Los = los,
                        UnitUuid = new List<string>(),
                        OutputMode = "Availability",
                        ChannelUuid = property.Uuid,
                        FilterByProducts = new List<string>(),
                        FilterByLegacyProducts = new List<string>(),
                        FilterByLegacyTariffs = new List<string>(),
                        FilterByTariffs = new List<string>()
                    }
                }
            };
        }


        /// <summary>
        /// Generates a human-readable test case name.
        /// </summary>
        private string GenerateTestCaseName(TestCaseParameters testCase, int index)
        {
            var occupancyDesc = testCase.Occupancy != null && testCase.Occupancy.Count > 0
                ? string.Join(", ", testCase.Occupancy)
                : "Unknown";
            
            return $"{testCase.Los} night(s) - {occupancyDesc} - {testCase.From} to {testCase.Until}";
        }

        /// <summary>
        /// Compares a single test case between V1 and V2 APIs.
        /// </summary>
        private async Task<ComparisonTestCaseResult> CompareTestCaseAsync(
            PropertyTestCase property, 
            TestCaseWithName testCase,
            bool includeExplanations)
        {
            var parameters = testCase.Parameters;
            var result = new ComparisonTestCaseResult
            {
                Oid = property.Oid,
                Uuid = property.Uuid,
                Description = property.Description,
                TestCaseName = testCase.Name, // Use name from test-cases.json
                Occupancy = parameters.Occupancy,
                BuildLevel = parameters.BuildLevel,
                From = parameters.From,
                Until = parameters.Until,
                Los = parameters.Los
            };

            try
            {
                // Build API call envelope
                var envelope = BuildApiCallEnvelope(property.Uuid, testCase);

                // Configure JSON serializer to handle missing types and assemblies gracefully
                var jsonSettings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    SerializationBinder = new SafeSerializationBinder(), // Handle missing assemblies during type resolution
                    Error = (sender, args) =>
                    {
                        // Handle all deserialization errors gracefully
                        var errorType = args.ErrorContext.Error.GetType().Name;
                        _logger.LogWarning("JSON deserialization {ErrorType}: {Error}", 
                            errorType, args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true;
                    }
                };

                // Call V1 API and measure only the HTTP call time
                var v1Stopwatch = Stopwatch.StartNew();
                var v1Response = await CallApiAsync(_v1BaseUrl, envelope);
                v1Stopwatch.Stop();
                result.V1ExecutionTimeMs = v1Stopwatch.ElapsedMilliseconds;
                var v1Data = JsonConvert.DeserializeObject<ApiResult<V1Response>>(v1Response, jsonSettings);
                
                // Call V2 API and measure only the HTTP call time
                var v2Stopwatch = Stopwatch.StartNew();
                var v2Response = await CallApiAsync(_v2BaseUrl, envelope);
                v2Stopwatch.Stop();
                result.V2ExecutionTimeMs = v2Stopwatch.ElapsedMilliseconds;
                var v2Data = JsonConvert.DeserializeObject<ApiResult<TOCC.Contracts.IBE.Models.Availability.Response>>(v2Response, jsonSettings);

                // Compare (comparer auto-configures with default skip paths)
                var comparer = new AvailabilityComparer();
                bool areEqual = comparer.Compare(v1Data.Value, v2Data.Value);
                
                result.Success = areEqual;
                result.DifferenceCount = comparer.Differences.Count;

                if (comparer.Differences.Count > 0)
                {
                    // Use the Difference objects directly - they already have the correct format
                    result.Differences = comparer.Differences;
                    
                    // Generate business-friendly differences only if requested (via explain=true)
                    if (includeExplanations)
                    {
                        result.BusinessFriendlyDifferences = comparer.Differences
                            .Select(d => BusinessFriendlyMapper.MapToBusinessFriendly(d))
                            .OrderByDescending(d => d.Severity) // Critical first
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing test case for property {Oid}", property.Oid);
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Builds API call envelope from test case parameters using injected builder.
        /// </summary>
        private ApiCallEnvelope BuildApiCallEnvelope(string channelUuid, TestCaseWithName testCase)
        {
            return _envelopeBuilder.BuildEnvelope(channelUuid, testCase.Parameters);
        }

        /// <summary>
        /// Calls the API using the envelope pattern.
        /// </summary>
        private async Task<string> CallApiAsync(string baseUrl, ApiCallEnvelope envelope)
        {
            var url = $"{baseUrl.TrimEnd('/')}/api/__api_party/ibe";
            
            // Configure JSON serializer to convert enums to strings
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() },
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

            _logger.LogDebug("Calling API: {Url}", url);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

    }
}
