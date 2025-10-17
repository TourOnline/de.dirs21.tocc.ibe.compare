using System.Collections.Generic;
using TOCC.IBE.Compare.Models.Core;

namespace TOCC.IBE.Compare.Models.Common
{
    /// <summary>
    /// Interface for custom property comparers.
    /// Implement this interface to create custom comparison logic for specific properties.
    /// </summary>
    public interface ICustomComparer
    {
        /// <summary>
        /// Compares two values and returns true if they are considered equal.
        /// </summary>
        /// <param name="valueV1">Value from V1 (expected)</param>
        /// <param name="valueV2">Value from V2 (actual)</param>
        /// <param name="path">The property path for error reporting</param>
        /// <param name="differences">List to add differences to if values don't match</param>
        /// <returns>True if values match, false otherwise</returns>
        bool Compare(object? valueV1, object? valueV2, string path, List<Difference> differences);
    }
}
