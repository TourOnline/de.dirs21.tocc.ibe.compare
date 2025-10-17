using System.Collections.Generic;
using TOCC.Contracts.IBE.Models.Availability;
using TOCC.IBE.Compare.Models.Core;
using TOCC.IBE.Compare.Models.V1;

namespace TOCC.IBE.Compare.Interfaces
{
    /// <summary>
    /// Interface for comparing availability responses between V1 and V2 formats.
    /// </summary>
    public interface IAvailabilityComparer
    {
        /// <summary>
        /// Gets the list of differences found during the last comparison.
        /// </summary>
        List<Difference> Differences { get; }

        /// <summary>
        /// Compares two availability responses and identifies differences.
        /// </summary>
        /// <param name="responseV1">The V1 response (expected).</param>
        /// <param name="responseV2">The V2 response (actual).</param>
        /// <returns>True if the responses are equal; otherwise, false.</returns>
        bool Compare(V1Response responseV1, Response responseV2);
    }
}
