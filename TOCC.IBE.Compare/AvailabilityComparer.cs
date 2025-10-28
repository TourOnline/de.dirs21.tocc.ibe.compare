using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TOCC.Contracts.IBE.Models.Availability;
using TOCC.IBE.Compare.Interfaces;
using TOCC.IBE.Compare.Models.Attributes;
using TOCC.IBE.Compare.Models.Common;
using TOCC.IBE.Compare.Models.Core;
using TOCC.IBE.Compare.Models.V1;

namespace TOCC.IBE.Compare
{
    /// <summary>
    /// Compares two availability response objects (V1 and V2) and identifies differences.
    /// </summary>
    public class AvailabilityComparer : IAvailabilityComparer
    {
        #region Constants

        private const int MaxDepth = 300;
        private static readonly string[] CommonIdProperties = { "_uuid", "_id", "Id", "Guid", "UUID", "Name" };
        private static readonly System.Text.RegularExpressions.Regex ArrayIndexRegex = new System.Text.RegularExpressions.Regex(@"\[[^\]]+\]", System.Text.RegularExpressions.RegexOptions.Compiled);

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AvailabilityComparer"/> class.
        /// Configures default skip paths for known differences between V1 and V2 APIs.
        /// </summary>
        public AvailabilityComparer()
        {
            Differences = new List<Difference>();
            
            // Configure default paths to skip during comparison
            // These represent known structural differences between V1 and V2 APIs
            SkipPaths = new List<string>
            {
                // Skip specific Ticks properties under Offers
                "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.MinStayThrough",
                "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.MinLos",
                "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.IsCta",
                "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.IsCtd",
                "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.IsMixable",
                "Result.Properties.Periods.Sets.Products.Ticks.InnerTicks",
                // Skip cache-related properties
                "Result.Properties.Periods.CacheSetName",
                "Result.Properties.Periods.IsFromCache",
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of differences found during the last comparison.
        /// Expected values are from V1, actual values are from V2.
        /// </summary>
        public List<Difference> Differences { get; }

        /// <summary>
        /// Gets or sets the list of paths to skip during comparison.
        /// Paths should be in dot notation (e.g., "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.MinStayThrough").
        /// Supports wildcard matching with array indices removed (e.g., "Result.Properties[0].Periods[0]" matches "Result.Properties.Periods").
        /// </summary>
        public List<string> SkipPaths { get; set; } = new List<string>();

        /// <summary>
        /// Cache for property information to avoid repeated reflection calls.
        /// </summary>
        private readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new Dictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// Cache for property lookup by name to avoid O(n) searches.
        /// </summary>
        private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _propertyLookupCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Compares two availability responses and identifies differences.
        /// </summary>
        /// <param name="responseV1">The V1 response (expected).</param>
        /// <param name="responseV2">The V2 response (actual).</param>
        /// <returns>True if the responses are equal; otherwise, false.</returns>
        public bool Compare(V1Response responseV1, Response responseV2)
        {
            Differences.Clear();
            _propertyCache.Clear();
            _propertyLookupCache.Clear();
            CompareObjects(responseV1, responseV2, responseV1?.GetType(), responseV2?.GetType(), string.Empty, 0);
            return Differences.Count == 0;
        }

        #endregion

        #region Private Methods - Object Comparison

        /// <summary>
        /// Compares two objects recursively.
        /// </summary>
        private bool CompareObjects(object objV1, object objV2, Type typeV1, Type typeV2, string path, int depth)
        {
            // Check depth limit
            if (depth > MaxDepth)
            {
                Differences.Add(new Difference($"{path}.Depth", $"MaxDepth>{MaxDepth}", null, DifferenceType.Depth));
                return false;
            }

            // Handle null cases
            if (objV1 == null && objV2 == null)
                return true;

            if (objV1 == null || objV2 == null)
            {
                Differences.Add(new Difference(path, objV1, objV2, DifferenceType.ValueMismatch));
                return false;
            }

            var propsV1 = GetCachedProperties(typeV1);
            var propsV2 = GetCachedProperties(typeV2);
            var propsV2Lookup = GetCachedPropertyLookup(typeV2);

            // Compare properties in V1
            foreach (var propV1 in propsV1)
            {
                var currentPath = string.IsNullOrEmpty(path) ? propV1.Name : $"{path}.{propV1.Name}";

                // Skip if path is in the skip list
                if (IsPathSkipped(currentPath))
                    continue;

                // Skip if marked with SkipValidationAttribute
                if (propV1.IsDefined(typeof(SkipValidationAttribute), true))
                    continue;

                propsV2Lookup.TryGetValue(propV1.Name, out var propV2);
                if (propV2 == null)
                {
                    Differences.Add(new Difference(currentPath, "<exists>", "<missing>", DifferenceType.MissingInV2));
                    continue;
                }

                var valueV1 = propV1.GetValue(objV1);
                var valueV2 = propV2.GetValue(objV2);

                // Priority 1: CustomCompareAttribute
                var customCompareAttr = propV1.GetCustomAttribute<CustomCompareAttribute>(true);
                if (customCompareAttr != null)
                {
                    var customComparer = CreateCustomComparer(customCompareAttr);
                    if (customComparer != null)
                    {
                        customComparer.Compare(valueV1, valueV2, currentPath, Differences);
                        continue;
                    }
                }

                // Priority 2: Default comparison
                CompareValues(valueV1, valueV2, currentPath, depth + 1, propV1);
            }

            // Check for properties in V2 that don't exist in V1
            var propsV1Lookup = GetCachedPropertyLookup(typeV1);
            foreach (var propV2 in propsV2)
            {
                propsV1Lookup.TryGetValue(propV2.Name, out var propV1);
                if (propV1 == null)
                {
                    var currentPath = string.IsNullOrEmpty(path) ? propV2.Name : $"{path}.{propV2.Name}";
                    
                    // Skip if path is in the skip list
                    if (IsPathSkipped(currentPath))
                        continue;
                    
                    var valueV2 = propV2.GetValue(objV2);
                    
                    // Skip if property is missing in V1 and null in V2
                    if (valueV2 == null)
                        continue;
                    
                    Differences.Add(new Difference(currentPath, "<missing>", valueV2, DifferenceType.MissingInV1));
                }
            }

            return Differences.Count == 0;
        }

        #endregion

        #region Private Methods - Value Comparison

        /// <summary>
        /// Compares two values with optional property info for attribute checking.
        /// </summary>
        private bool CompareValues(object valueV1, object valueV2, string path, int depth, PropertyInfo propertyInfo = null)
        {
            // Handle null cases
            if (valueV1 == null && valueV2 == null)
                return true;

            if (valueV1 == null || valueV2 == null)
            {
                Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            // Handle dictionaries - compare by key
            if (valueV1 is IDictionary dict1 && valueV2 is IDictionary dict2)
            {
                return CompareDictionaries(dict1, dict2, path, depth);
            }

            // Handle collections
            if (valueV1 is IEnumerable enumV1 && valueV2 is IEnumerable enumV2 && !(valueV1 is string))
            {
                var uniqueIdAttr = propertyInfo?.GetCustomAttribute<UniqueIdentifierAttribute>(true);

                if (uniqueIdAttr != null)
                {
                    return CompareCollectionsByUniqueId(enumV1, enumV2, path, depth, uniqueIdAttr.PropertyName);
                }
                else
                {
                    return CompareCollectionsByIndex(enumV1, enumV2, path, depth, propertyInfo);
                }
            }

            var type1 = valueV1.GetType();
            var type2 = valueV2.GetType();

            // Handle Enum and String comparison
            // If either type is an enum or string, convert both to strings and compare
            // This handles: enum-to-enum, enum-to-string, string-to-enum
            if (type1.IsEnum || type2.IsEnum || type1 == typeof(string) || type2 == typeof(string))
            {
                var str1 = valueV1.ToString();
                var str2 = valueV2.ToString();
                
                if (!string.Equals(str1, str2, StringComparison.Ordinal))
                {
                    Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                    return false;
                }
                
                return true;
            }

            // Handle DateTime and DateTimeOffset
            if ((valueV1 is DateTime || valueV1 is DateTimeOffset) && (valueV2 is DateTime || valueV2 is DateTimeOffset))
            {
                if (!valueV1.Equals(valueV2))
                {
                    Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                    return false;
                }
                return true;
            }
            else if (valueV1 is DateTime || valueV1 is DateTimeOffset)
            {
                // Type mismatch
                Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            // Handle TimeSpan
            if (valueV1 is TimeSpan ts1 && valueV2 is TimeSpan ts2)
            {
                if (ts1 != ts2)
                {
                    Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                    return false;
                }
                return true;
            }

            // Handle Nullable types - unwrap and compare
            var underlyingType1 = Nullable.GetUnderlyingType(type1);
            var underlyingType2 = Nullable.GetUnderlyingType(type2);
            
            if (underlyingType1 != null || underlyingType2 != null)
            {
                // At least one is nullable - both values are already boxed, just compare directly
                // For nullable enums, valueV1 and valueV2 are already the enum values (not Nullable<T>)
                // Ensure both types match before comparing values
                var actualType1 = underlyingType1 ?? type1;
                var actualType2 = underlyingType2 ?? type2;
                
                if (actualType1 != actualType2)
                {
                    Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                    return false;
                }
                
                if (!valueV1.Equals(valueV2))
                {
                    Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                    return false;
                }
                return true;
            }

            // Handle primitive types, strings, decimals, and Guids
            if (type1.IsPrimitive || valueV1 is string || valueV1 is decimal || valueV1 is Guid)
            {
                if (type1 != type2)
                {
                    try
                    {
                        if (type1.IsPrimitive && type2.IsPrimitive)
                        {
                            var decimalV1 = Convert.ToDecimal(valueV1);
                            var decimalV2 = Convert.ToDecimal(valueV2);
                            if (decimalV1 != decimalV2)
                            {
                                Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                                return false;
                            }
                            return true;
                        }
                    }
                    catch
                    {
                        Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                        return false;
                    }

                    Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                    return false;
                }

                if (!valueV1.Equals(valueV2))
                {
                    Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                    return false;
                }
                return true;
            }

            // Optimize struct comparison - use Equals() if both are the same struct type
            if (type1.IsValueType && type2.IsValueType && type1 == type2)
            {
                if (!valueV1.Equals(valueV2))
                {
                    Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                    return false;
                }
                return true;
            }

            // Compare complex objects recursively
            return CompareObjects(valueV1, valueV2, valueV1.GetType(), valueV2.GetType(), path, depth + 1);
        }

        #endregion

        #region Private Methods - Collection Comparison

        /// <summary>
        /// Compares two dictionaries by matching keys.
        /// </summary>
        private bool CompareDictionaries(IDictionary dict1, IDictionary dict2, string path, int depth)
        {
            if (dict1.Count != dict2.Count)
            {
                Differences.Add(new Difference($"{path}.Count", dict1.Count, dict2.Count, DifferenceType.Count));
                return false;
            }

            bool allMatch = true;

            foreach (var key in dict1.Keys)
            {
                var keyPath = $"{path}[{key}]";

                if (!dict2.Contains(key))
                {
                    Differences.Add(new Difference(keyPath, dict1[key], "<missing>", DifferenceType.MissingInV2));
                    allMatch = false;
                    continue;
                }

                var value1 = dict1[key];
                var value2 = dict2[key];
                CompareValues(value1, value2, keyPath, depth + 1);
            }

            foreach (var key in dict2.Keys)
            {
                if (!dict1.Contains(key))
                {
                    var keyPath = $"{path}[{key}]";
                    Differences.Add(new Difference(keyPath, "<missing>", dict2[key], DifferenceType.MissingInV1));
                    allMatch = false;
                }
            }

            return allMatch;
        }

        /// <summary>
        /// Compares collections by index (default behavior).
        /// Generates human-readable paths using unique identifiers when available.
        /// </summary>
        private bool CompareCollectionsByIndex(IEnumerable enumV1, IEnumerable enumV2, string path, int depth, PropertyInfo collectionProperty = null)
        {
            var listV1 = enumV1.Cast<object>().ToList();
            var listV2 = enumV2.Cast<object>().ToList();

            if (listV1.Count != listV2.Count)
            {
                Differences.Add(new Difference($"{path}.Count", listV1.Count, listV2.Count, DifferenceType.Count));
                return false;
            }

            // Check if collection property has UniqueIdentifierAttribute
            string uniqueIdPropertyName = null;
            if (collectionProperty != null)
            {
                var uniqueIdAttr = collectionProperty.GetCustomAttribute<UniqueIdentifierAttribute>(true);
                if (uniqueIdAttr != null)
                {
                    uniqueIdPropertyName = uniqueIdAttr.PropertyName;
                }
            }

            for (int i = 0; i < listV1.Count; i++)
            {
                // Generate human-readable path using unique identifier if available
                var itemPath = GenerateHumanReadablePath(path, listV1[i], listV2[i], i, uniqueIdPropertyName);
                CompareValues(listV1[i], listV2[i], itemPath, depth + 1);
            }

            return true;
        }

        /// <summary>
        /// Compares collections by matching items using a unique identifier property.
        /// </summary>
        private bool CompareCollectionsByUniqueId(IEnumerable enumV1, IEnumerable enumV2, string path, int depth, string uniqueIdPropertyName)
        {
            var listV1 = enumV1.Cast<object>().ToList();
            var listV2 = enumV2.Cast<object>().ToList();

            if (listV1.Count != listV2.Count)
            {
                Differences.Add(new Difference($"{path}.Count", listV1.Count, listV2.Count, DifferenceType.Count));
                return false;
            }

            // Build lookup dictionary for V2 items
            var dict2 = new Dictionary<object, object>();
            var nullItemsV2Count = 0;
            bool allMatch = true;
            
            foreach (var item2 in listV2)
            {
                if (item2 == null)
                {
                    nullItemsV2Count++;
                    continue;
                }

                var idValue = GetPropertyValue(item2, uniqueIdPropertyName);
                if (idValue != null)
                {
                    dict2[idValue] = item2;
                }
            }

            var nullItemsV1Count = 0;

            // Match each V1 item with corresponding V2 item
            foreach (var item1 in listV1)
            {
                if (item1 == null)
                {
                    nullItemsV1Count++;
                    continue;
                }

                var idValue = GetPropertyValue(item1, uniqueIdPropertyName);
                if (idValue == null)
                {
                    Differences.Add(new Difference(path, $"Item missing {uniqueIdPropertyName}", null, DifferenceType.ValueMismatch));
                    allMatch = false;
                    continue;
                }

                if (!dict2.TryGetValue(idValue, out var item2))
                {
                    Differences.Add(new Difference($"{path}[{uniqueIdPropertyName}={idValue}]", item1, "<missing>", DifferenceType.MissingInV2));
                    allMatch = false;
                    continue;
                }

                var itemPath = $"{path}[{uniqueIdPropertyName}={idValue}]";
                CompareValues(item1, item2, itemPath, depth + 1);

                dict2.Remove(idValue);
            }

            // Check for unmatched V2 items
            foreach (var kvp in dict2)
            {
                Differences.Add(new Difference($"{path}[{uniqueIdPropertyName}={kvp.Key}]", "<missing>", kvp.Value, DifferenceType.MissingInV1));
                allMatch = false;
            }

            // Compare null item counts
            if (nullItemsV1Count != nullItemsV2Count)
            {
                Differences.Add(new Difference($"{path}.NullItemCount", nullItemsV1Count, nullItemsV2Count, DifferenceType.Count));
                allMatch = false;
            }

            return allMatch;
        }

        #endregion

        #region Private Methods - Helpers

        /// <summary>
        /// Creates an instance of a custom comparer from the CustomCompareAttribute.
        /// </summary>
        private ICustomComparer CreateCustomComparer(CustomCompareAttribute attribute)
        {
            try
            {
                if (attribute.Parameters != null && attribute.Parameters.Length > 0)
                {
                    return (ICustomComparer)Activator.CreateInstance(attribute.ComparerType, attribute.Parameters);
                }
                else
                {
                    return (ICustomComparer)Activator.CreateInstance(attribute.ComparerType);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the value of a property by name using reflection.
        /// </summary>
        private object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null) return null;

            var type = obj.GetType();
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
            {
                var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
                return field?.GetValue(obj);
            }

            return property.GetValue(obj);
        }

        /// <summary>
        /// Generates a human-readable path for collection items using unique identifiers when available.
        /// Falls back to index if no unique identifier is found.
        /// </summary>
        /// <param name="basePath">The base path (e.g., "Result.Properties")</param>
        /// <param name="itemV1">The item from V1 collection</param>
        /// <param name="itemV2">The item from V2 collection</param>
        /// <param name="index">The index of the item in the collection</param>
        /// <param name="specifiedUniqueIdProperty">The unique identifier property name specified by UniqueIdentifierAttribute on the collection</param>
        /// <returns>A human-readable path string</returns>
        private string GenerateHumanReadablePath(string basePath, object itemV1, object itemV2, int index, string specifiedUniqueIdProperty = null)
        {
            // Try to get unique identifier from either item (prefer V1)
            var item = itemV1 ?? itemV2;
            if (item == null)
                return $"{basePath}[{index}]";

            var itemType = item.GetType();

            // Priority 0: Use specified unique identifier from collection's UniqueIdentifierAttribute
            if (!string.IsNullOrEmpty(specifiedUniqueIdProperty))
            {
                var specifiedProp = itemType.GetProperty(specifiedUniqueIdProperty, BindingFlags.Public | BindingFlags.Instance);
                if (specifiedProp != null)
                {
                    var idValue = specifiedProp.GetValue(item);
                    if (idValue != null && !IsDefaultValue(idValue))
                    {
                        return $"{basePath}[{specifiedProp.Name}={idValue}]";
                    }
                }
            }

            // Priority 1: Check for UniqueIdentifierAttribute on properties
            var properties = GetCachedProperties(itemType);
            foreach (var prop in properties)
            {
                var uniqueIdAttr = prop.GetCustomAttribute<TOCC.IBE.Compare.Models.Attributes.UniqueIdentifierAttribute>();
                if (uniqueIdAttr != null)
                {
                    var idValue = prop.GetValue(item);
                    if (idValue != null)
                    {
                        return $"{basePath}[{prop.Name}={idValue}]";
                    }
                }
            }

            // Priority 2: Check for common unique identifier properties (fallback)
            foreach (var propName in CommonIdProperties)
            {
                var prop = properties.FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
                if (prop == null)
                    continue;

                // Skip collection properties (List, IEnumerable, etc.)
                // Check if the property type implements IEnumerable (but not string)
                if (prop.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                    continue;

                try
                {
                    var idValue = prop.GetValue(item);
                    
                    // Skip null values
                    if (idValue == null)
                        continue;
                    
                    // Also check if the actual value is a collection (double-check)
                    if (idValue is IEnumerable && !(idValue is string))
                        continue;
                    
                    // Extra check: skip if the value's type name suggests it's a collection
                    var valueTypeName = idValue.GetType().FullName ?? "";
                    if (valueTypeName.Contains("List") || valueTypeName.Contains("Collection") || valueTypeName.Contains("[]"))
                        continue;
                    
                    // Skip default values (0, Guid.Empty, etc.)
                    if (IsDefaultValue(idValue))
                        continue;
                    
                    // Found a valid identifier!
                    return $"{basePath}[{prop.Name}={idValue}]";
                }
                catch
                {
                    // If we can't get the value, skip this property
                    continue;
                }
            }

            // Fallback to index
            return $"{basePath}[{index}]";
        }

        /// <summary>
        /// Checks if a value is the default value for its type.
        /// </summary>
        private bool IsDefaultValue(object value)
        {
            if (value == null)
                return true;

            var type = value.GetType();
            if (type.IsValueType)
            {
                var defaultValue = Activator.CreateInstance(type);
                return value.Equals(defaultValue);
            }

            return false;
        }

        /// <summary>
        /// Checks if a path should be skipped based on the SkipPaths configuration.
        /// Supports wildcard matching by removing array indices from paths.
        /// </summary>
        /// <param name="path">The path to check (e.g., "Result.Properties[0].Periods[0].Sets[0]")</param>
        /// <returns>True if the path should be skipped; otherwise, false.</returns>
        private bool IsPathSkipped(string path)
        {
            if (SkipPaths == null || SkipPaths.Count == 0)
                return false;

            // Normalize the path by removing array indices
            // "Result.Properties[0].Periods[0]" becomes "Result.Properties.Periods"
            var normalizedPath = ArrayIndexRegex.Replace(path, "");

            // Check for exact match or prefix match
            foreach (var skipPath in SkipPaths)
            {
                // Exact match
                if (normalizedPath.Equals(skipPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                // Prefix match (path starts with skipPath)
                if (normalizedPath.StartsWith(skipPath + ".", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets cached property information for a type.
        /// </summary>
        private PropertyInfo[] GetCachedProperties(Type type)
        {
            if (!_propertyCache.TryGetValue(type, out var properties))
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                _propertyCache[type] = properties;
            }
            return properties;
        }

        /// <summary>
        /// Gets cached property lookup dictionary for a type.
        /// </summary>
        private Dictionary<string, PropertyInfo> GetCachedPropertyLookup(Type type)
        {
            if (!_propertyLookupCache.TryGetValue(type, out var lookup))
            {
                var properties = GetCachedProperties(type);
                lookup = new Dictionary<string, PropertyInfo>(properties.Length);
                foreach (var prop in properties)
                {
                    lookup[prop.Name] = prop;
                }
                _propertyLookupCache[type] = lookup;
            }
            return lookup;
        }

        #endregion
    }
}
