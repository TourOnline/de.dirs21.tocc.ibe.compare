using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TOCC.IBE.Compare.Models.Common;
using TOCC.IBE.Compare.Server.Models;
using TOCC.IBE.Compare.Server.Services;
using ComparisonRequest = TOCC.IBE.Compare.Models.Common.ComparisonRequest;
using PropertyTestCase = TOCC.IBE.Compare.Models.Common.PropertyTestCase;
using TestCaseParameters = TOCC.IBE.Compare.Models.Common.TestCaseParameters;

namespace TOCC.IBE.Compare.Server.Controllers
{
    /// <summary>
    /// Controller for comparing V1 and V2 API responses.
    /// Production-ready, cross-platform compatible.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ComparisonController : ControllerBase
    {
        private readonly IComparisonService _comparisonService;
        private readonly ILogger<ComparisonController> _logger;

        public ComparisonController(
            IComparisonService comparisonService,
            ILogger<ComparisonController> logger)
        {
            _comparisonService = comparisonService ?? throw new ArgumentNullException(nameof(comparisonService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes comparison tests for the provided properties and test cases.
        /// Use ?explain=true query parameter to include business-friendly explanations for non-technical users.
        /// </summary>
        /// <param name="request">Comparison request containing properties and test cases</param>
        /// <param name="explain">If true, includes business-friendly explanations for differences (default: false)</param>
        /// <returns>Comparison results summary with optional business-friendly differences</returns>
        /// <response code="200">Comparison completed successfully</response>
        /// <response code="400">Invalid request or validation errors</response>
        /// <response code="500">Internal server error during comparison</response>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(ComparisonResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ComparisonResponse>> ExecuteComparison(
            [FromBody] ComparisonRequest request,
            [FromQuery] bool explain = false)
        {
            try
            {
                _logger.LogInformation("Received comparison request with {PropertyCount} properties", 
                    request?.Properties?.Count ?? 0);

                // Validate request
                var validationErrors = ValidateRequest(request);
                if (validationErrors.Any())
                {
                    _logger.LogWarning("Validation failed: {Errors}", string.Join(", ", validationErrors));
                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = validationErrors
                    });
                }

                // Execute comparison with optional explanations
                var response = await _comparisonService.ExecuteComparisonAsync(request!, explain);

                _logger.LogInformation("Comparison completed: {Success}/{Total} successful",
                    response.SuccessfulComparisons, response.TotalTestCases);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuration error during comparison");
                return BadRequest(new
                {
                    success = false,
                    message = $"Configuration error: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during comparison");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "An unexpected error occurred during comparison",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Validates the comparison request.
        /// </summary>
        /// <param name="request">Request to validate</param>
        /// <returns>List of validation error messages</returns>
        private List<string> ValidateRequest(ComparisonRequest? request)
        {
            var errors = new List<string>();

            if (request == null)
            {
                errors.Add("Request body is required");
                return errors;
            }

            if (request.Properties == null || request.Properties.Count == 0)
            {
                errors.Add("At least one property is required");
                return errors;
            }

            for (int i = 0; i < request.Properties.Count; i++)
            {
                var property = request.Properties[i];
                var propertyPrefix = $"Properties[{i}]";

                // Validate required fields
                if (string.IsNullOrWhiteSpace(property.Oid))
                {
                    errors.Add($"{propertyPrefix}: _oid is required");
                }

                if (string.IsNullOrWhiteSpace(property.Uuid))
                {
                    errors.Add($"{propertyPrefix}: _uuid is required");
                }

                if (string.IsNullOrWhiteSpace(property.UsePreDefinedTestCases))
                {
                    errors.Add($"{propertyPrefix}: UsePreDefinedTestCases is required");
                }

                // Validate UsePreDefinedTestCases value
                bool usePreDefined = string.Equals(property.UsePreDefinedTestCases, "true", StringComparison.OrdinalIgnoreCase);
                bool isValidValue = usePreDefined || string.Equals(property.UsePreDefinedTestCases, "false", StringComparison.OrdinalIgnoreCase);

                if (!isValidValue)
                {
                    errors.Add($"{propertyPrefix}: UsePreDefinedTestCases must be 'true' or 'false'");
                }

                // If UsePreDefinedTestCases is false, validate TestCases
                if (!usePreDefined)
                {
                    if (property.TestCases == null || property.TestCases.Count == 0)
                    {
                        errors.Add($"{propertyPrefix}: When UsePreDefinedTestCases is 'false', at least one TestCase is required");
                    }
                    else
                    {
                        // Validate each test case
                        for (int j = 0; j < property.TestCases.Count; j++)
                        {
                            var testCase = property.TestCases[j];
                            var testCasePrefix = $"{propertyPrefix}.TestCases[{j}]";

                            ValidateTestCase(testCase, testCasePrefix, errors);
                        }
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Validates a single test case.
        /// </summary>
        private void ValidateTestCase(TestCaseParameters testCase, string prefix, List<string> errors)
        {
            if (testCase.Occupancy == null || testCase.Occupancy.Count == 0)
            {
                errors.Add($"{prefix}: Occupancy is required and must contain at least one entry");
            }

            if (string.IsNullOrWhiteSpace(testCase.BuildLevel))
            {
                errors.Add($"{prefix}: BuildLevel is required");
            }

            if (string.IsNullOrWhiteSpace(testCase.From))
            {
                errors.Add($"{prefix}: From date is required");
            }
            else if (!DateTime.TryParse(testCase.From, out _))
            {
                errors.Add($"{prefix}: From must be a valid date (yyyy-MM-dd format recommended)");
            }

            if (string.IsNullOrWhiteSpace(testCase.Until))
            {
                errors.Add($"{prefix}: Until date is required");
            }
            else if (!DateTime.TryParse(testCase.Until, out _))
            {
                errors.Add($"{prefix}: Until must be a valid date (yyyy-MM-dd format recommended)");
            }

            if (testCase.Los <= 0)
            {
                errors.Add($"{prefix}: Los (Length of Stay) must be greater than 0");
            }

            if (string.IsNullOrWhiteSpace(testCase.OutputMode))
            {
                errors.Add($"{prefix}: OutputMode is required");
            }

            if (string.IsNullOrWhiteSpace(testCase.ChannelUuid))
            {
                errors.Add($"{prefix}: Channel_uuid is required");
            }

            // Validate date logic
            if (!string.IsNullOrWhiteSpace(testCase.From) && !string.IsNullOrWhiteSpace(testCase.Until))
            {
                if (DateTime.TryParse(testCase.From, out var fromDate) && 
                    DateTime.TryParse(testCase.Until, out var untilDate))
                {
                    if (untilDate <= fromDate)
                    {
                        errors.Add($"{prefix}: Until date must be after From date");
                    }

                    var daysDiff = (untilDate - fromDate).Days;
                    if (daysDiff != testCase.Los)
                    {
                        errors.Add($"{prefix}: Los ({testCase.Los}) must match the difference between From and Until dates ({daysDiff} days)");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the health status of the comparison service.
        /// </summary>
        /// <returns>Health status information</returns>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<object> GetHealth()
        {
            return Ok(new
            {
                service = "ComparisonController",
                status = "healthy",
                timestamp = DateTime.UtcNow.ToString("O"),
                version = "1.0.0"
            });
        }
    }
}
