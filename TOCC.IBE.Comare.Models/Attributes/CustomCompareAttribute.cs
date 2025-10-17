using System;
using TOCC.IBE.Compare.Models.Common;

namespace TOCC.IBE.Compare.Models.Attributes
{
    /// <summary>
    /// Attribute to specify a custom comparer for a property.
    /// Apply this to properties in V1 models to use custom comparison logic.
    /// </summary>
    /// <example>
    /// [CustomCompare(typeof(DateOnlyComparer))]
    /// public DateTime CreatedDate { get; set; }
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class CustomCompareAttribute : Attribute
    {
        /// <summary>
        /// The type of the custom comparer. Must implement ICustomComparer.
        /// </summary>
        public Type ComparerType { get; }

        /// <summary>
        /// Optional parameters to pass to the comparer constructor.
        /// </summary>
        public object[] Parameters { get; set; }

        /// <summary>
        /// Creates a new CustomCompareAttribute with the specified comparer type.
        /// </summary>
        /// <param name="comparerType">Type that implements ICustomComparer</param>
        public CustomCompareAttribute(Type comparerType)
        {
            if (comparerType == null)
                throw new ArgumentNullException(nameof(comparerType));

            if (!typeof(ICustomComparer).IsAssignableFrom(comparerType))
                throw new ArgumentException($"Type {comparerType.Name} must implement ICustomComparer", nameof(comparerType));

            ComparerType = comparerType;
        }

        /// <summary>
        /// Creates a new CustomCompareAttribute with the specified comparer type and parameters.
        /// </summary>
        /// <param name="comparerType">Type that implements ICustomComparer</param>
        /// <param name="parameters">Parameters to pass to the comparer constructor</param>
        public CustomCompareAttribute(Type comparerType, params object[] parameters) : this(comparerType)
        {
            Parameters = parameters;
        }
    }
}
