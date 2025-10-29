using System.Threading.Tasks;
using TOCC.IBE.Compare.Models.Common;
using TOCC.IBE.Compare.Server.Models;
using ComparisonRequest = TOCC.IBE.Compare.Models.Common.ComparisonRequest;

namespace TOCC.IBE.Compare.Server.Services
{
    /// <summary>
    /// Interface for comparison service operations.
    /// </summary>
    public interface IComparisonService
    {
        /// <summary>
        /// Executes comparison for the given request.
        /// </summary>
        /// <param name="request">Comparison request containing properties and test cases</param>
        /// <param name="includeExplanations">If true, includes business-friendly explanations for differences</param>
        /// <returns>Comparison response with results</returns>
        Task<ComparisonResponse> ExecuteComparisonAsync(ComparisonRequest request, bool includeExplanations = false);
    }
}
