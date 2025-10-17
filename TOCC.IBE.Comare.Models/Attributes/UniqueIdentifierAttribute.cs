using System;

namespace TOCC.IBE.Compare.Models.Attributes
{
    /// <summary>
    /// Attribute to specify the unique identifier property for collection item matching.
    /// When comparing collections, items will be matched by this property instead of by index.
    /// </summary>
    /// <example>
    /// [UniqueIdentifier("_uuid")]
    /// public List&lt;Product&gt; Products { get; set; }
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class UniqueIdentifierAttribute : Attribute
    {
        /// <summary>
        /// The name of the property to use as unique identifier for matching collection items.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Creates a new UniqueIdentifierAttribute with the specified property name.
        /// </summary>
        /// <param name="propertyName">Name of the property to use as unique identifier (e.g., "_uuid", "Id", "Key")</param>
        public UniqueIdentifierAttribute(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

            PropertyName = propertyName;
        }
    }
}
