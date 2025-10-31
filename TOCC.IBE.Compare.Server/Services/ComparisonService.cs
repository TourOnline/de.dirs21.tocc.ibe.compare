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
        private readonly string _frontendRoute;
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
            _frontendRoute = _configuration["IntegrationTest:FrontendRoute"] ?? "result";
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

                var propertySummaries = new Dictionary<string, PropertyUrlSummary>();

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

                        // Maintain property-level URL summary when directory becomes available
                        if (!string.IsNullOrWhiteSpace(result.Directory))
                        {
                            if (!propertySummaries.TryGetValue(property.Oid, out var summary))
                            {
                                summary = new PropertyUrlSummary
                                {
                                    Oid = property.Oid,
                                    Uuid = property.Uuid,
                                    Directory = result.Directory,
                                    V1Url = BuildPropertyUrl(_v1BaseUrl, result.Directory!),
                                    V2Url = BuildPropertyUrl(_v2BaseUrl, result.Directory!)
                                };
                                propertySummaries[property.Oid] = summary;
                            }
                        }

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

                // Set properties list on response
                response.Properties = propertySummaries.Values.ToList();

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

             
                // Call V1 API and measure only the HTTP call time
                var v1Stopwatch = Stopwatch.StartNew();
                var v1Response = await CallApiAsync(_v1BaseUrl, envelope);
                v1Stopwatch.Stop();
                result.V1ExecutionTimeMs = v1Stopwatch.ElapsedMilliseconds;
                result.V1ResponseJson = v1Response; // Store raw JSON for integration test artifacts
                var v1Data = JsonConvert.DeserializeObject<ApiResult<V1Response>>(v1Response);
                var directory = v1Data?.Value?.Result?.Properties?.FirstOrDefault()?.Directory;
                result.Directory = directory;
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    result.V1Url = BuildTestCaseUrl(_v1BaseUrl, directory!, parameters);
                    result.V2Url = BuildTestCaseUrl(_v2BaseUrl, directory!, parameters);
                }
                
                // Call V2 API and measure only the HTTP call time
                var v2Stopwatch = Stopwatch.StartNew();
                var v2Response = await CallApiAsync(_v2BaseUrl, envelope);
                v2Stopwatch.Stop();
                result.V2ExecutionTimeMs = v2Stopwatch.ElapsedMilliseconds;
                result.V2ResponseJson = v2Response; // Store raw JSON for integration test artifacts
                var v2Data = JsonConvert.DeserializeObject<ApiResult<TOCC.Contracts.IBE.Models.Availability.Response>>(v2Response);

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

        private string BuildPropertyUrl(string baseUrl, string directory)
        {
            var path = $"{baseUrl.TrimEnd('/')}/{directory.Trim('/')}/{_frontendRoute.Trim('/')}";
            return path;
        }

        private string BuildTestCaseUrl(string baseUrl, string directory, TestCaseParameters p)
        {
            var path = BuildPropertyUrl(baseUrl, directory);
            var range = $"{p.From},{p.Until}";
            var sets = BuildSetsJson(p.Occupancy ?? new List<string>());
            var encodedSets = Uri.EscapeDataString(sets);
            var url = $"{path}?range={range}&sets={encodedSets}&los={p.Los}";
            return url;
        }

        private string BuildSetsJson(List<string> occupancy)
        {
            var sets = new List<object>();
            foreach (var occ in occupancy)
            {
                if (string.IsNullOrWhiteSpace(occ)) continue;
                var tokens = occ.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                int adultCount = tokens.Count(t => string.Equals(t, "a", StringComparison.OrdinalIgnoreCase));
                var children = new List<int>();
                foreach (var t in tokens)
                {
                    if (int.TryParse(t, out var age)) children.Add(age);
                }
                var set = new
                {
                    uuid = Guid.NewGuid(),
                    occupancy = new
                    {
                        adultCount = adultCount,
                        children = children
                    }
                };
                sets.Add(set);
            }
            return JsonConvert.SerializeObject(sets);
        }

    }
}
