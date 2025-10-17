using System;

namespace TOCC.IBE.Compare.Models.Core
{
    /// <summary>
    /// Represents a difference found during comparison between V1 and V2 objects.
    /// </summary>
    public class Difference
    {
        /// <summary>
        /// Gets the property path where the difference was found.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the expected value from V1.
        /// </summary>
        public object? Expected { get; }

        /// <summary>
        /// Gets the actual value from V2.
        /// </summary>
        public object? Actual { get; }

        /// <summary>
        /// Gets or sets the type of difference detected.
        /// </summary>
        public DifferenceType Type { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Difference"/> class.
        /// </summary>
        /// <param name="path">The property path where the difference was found.</param>
        /// <param name="expected">The expected value from V1.</param>
        /// <param name="actual">The actual value from V2.</param>
        /// <param name="type">The type of difference.</param>
        public Difference(string path, object? expected, object? actual, DifferenceType type)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Expected = expected;
            Actual = actual;
            Type = type;
        }

        /// <summary>
        /// Returns a string representation of the difference.
        /// </summary>
        public override string ToString()
        {
            return $"[{Type}] {Path}: Expected='{Expected}', Actual='{Actual}'";
        }
    }

    /// <summary>
    /// Defines the types of differences that can be detected during comparison.
    /// </summary>
    public enum DifferenceType
    {
        /// <summary>
        /// Values do not match.
        /// </summary>
        ValueMismatch,

        /// <summary>
        /// Property exists in V2 but is missing in V1.
        /// </summary>
        MissingInV1,

        /// <summary>
        /// Property exists in V1 but is missing in V2.
        /// </summary>
        MissingInV2,

        /// <summary>
        /// Collection counts do not match.
        /// </summary>
        Count,

        /// <summary>
        /// Maximum comparison depth exceeded.
        /// </summary>
        Depth,

        /// <summary>
        /// Custom validation failed.
        /// </summary>
        CustomValidationFailed
    }
}
