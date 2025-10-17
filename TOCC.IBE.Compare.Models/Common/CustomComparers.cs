using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TOCC.IBE.Compare.Models.Core;

namespace TOCC.IBE.Compare.Models.Common
{
    /// <summary>
    /// Example: Compares DateTime values ignoring the time component (date only).
    /// </summary>
    public class DateOnlyComparer : ICustomComparer
    {
        public bool Compare(object? valueV1, object? valueV2, string path, List<Difference> differences)
        {
            if (valueV1 == null && valueV2 == null)
                return true;

            if (valueV1 == null || valueV2 == null)
            {
                differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            if (valueV1 is DateTime dt1 && valueV2 is DateTime dt2)
            {
                if (dt1.Date != dt2.Date)
                {
                    differences.Add(new Difference(path, dt1.Date, dt2.Date, DifferenceType.ValueMismatch));
                    return false;
                }
                return true;
            }

            differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
            return false;
        }
    }

    /// <summary>
    /// Example: Compares decimal values with a specified tolerance.
    /// </summary>
    public class DecimalToleranceComparer : ICustomComparer
    {
        private readonly decimal _tolerance;

        public DecimalToleranceComparer(decimal tolerance = 0.01m)
        {
            _tolerance = tolerance;
        }

        public bool Compare(object? valueV1, object? valueV2, string path, List<Difference> differences)
        {
            if (valueV1 == null && valueV2 == null)
                return true;

            if (valueV1 == null || valueV2 == null)
            {
                differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            try
            {
                var dec1 = Convert.ToDecimal(valueV1);
                var dec2 = Convert.ToDecimal(valueV2);

                if (Math.Abs(dec1 - dec2) > _tolerance)
                {
                    differences.Add(new Difference(path, dec1, dec2, DifferenceType.ValueMismatch));
                    return false;
                }
                return true;
            }
            catch
            {
                differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }
        }
    }

    /// <summary>
    /// Example: Compares strings ignoring case.
    /// </summary>
    public class CaseInsensitiveStringComparer : ICustomComparer
    {
        public bool Compare(object? valueV1, object? valueV2, string path, List<Difference> differences)
        {
            if (valueV1 == null && valueV2 == null)
                return true;

            if (valueV1 == null || valueV2 == null)
            {
                differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            var str1 = valueV1.ToString();
            var str2 = valueV2.ToString();

            if (!string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase))
            {
                differences.Add(new Difference(path, str1, str2, DifferenceType.ValueMismatch));
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Example: Compares collections ignoring order (set comparison).
    /// </summary>
    public class UnorderedCollectionComparer : ICustomComparer
    {
        public bool Compare(object? valueV1, object? valueV2, string path, List<Difference> differences)
        {
            if (valueV1 == null && valueV2 == null)
                return true;

            if (valueV1 == null || valueV2 == null)
            {
                differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            if (valueV1 is System.Collections.IEnumerable enum1 && 
                valueV2 is System.Collections.IEnumerable enum2 &&
                !(valueV1 is string))
            {
                var list1 = new List<object>();
                var list2 = new List<object>();

                foreach (var item in enum1)
                    list1.Add(item);

                foreach (var item in enum2)
                    list2.Add(item);

                if (list1.Count != list2.Count)
                {
                    differences.Add(new Difference(path + ".Count", list1.Count, list2.Count, DifferenceType.Count));
                    return false;
                }

                // Check if all items in list1 exist in list2 (set comparison)
                var list2Copy = new List<object>(list2);
                foreach (var item1 in list1)
                {
                    bool found = false;
                    for (int i = 0; i < list2Copy.Count; i++)
                    {
                        if (Equals(item1, list2Copy[i]))
                        {
                            list2Copy.RemoveAt(i);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        differences.Add(new Difference(path, $"Item {item1} not found in V2", null, DifferenceType.ValueMismatch));
                        return false;
                    }
                }

                return true;
            }

            differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
            return false;
        }
    }

    /// <summary>
    /// Always considers values as equal (useful for ignoring specific properties).
    /// </summary>
    public class AlwaysEqualComparer : ICustomComparer
    {
        public bool Compare(object? valueV1, object? valueV2, string path, List<Difference> differences)
        {
            // Always return true - values are considered equal
            return true;
        }
    }

    /// <summary>
    /// Compares Price objects with custom logic.
    /// TODO: Implement actual price comparison logic.
    /// </summary>
    public class PriceComparer : ICustomComparer
    {
        public bool Compare(object? valueV1, object? valueV2, string path, List<Difference> differences)
        {
            // TODO: Implement price comparison logic
            // For now, always return true (skip comparison)
            return true;
        }
    }

    /// <summary>
    /// Compares PersonPrices collections with custom logic.
    /// TODO: Implement actual person prices comparison logic.
    /// </summary>
    public class PersonPricesComparer : ICustomComparer
    {
        public bool Compare(object? valueV1, object? valueV2, string path, List<Difference> differences)
        {
            // TODO: Implement person prices comparison logic
            // For now, always return true (skip comparison)
            return true;
        }
    }

    /// <summary>
    /// Example: Compares only if both values are not null (ignores null differences).
    /// </summary>
    public class IgnoreNullComparer : ICustomComparer
    {
        public bool Compare(object? valueV1, object? valueV2, string path, List<Difference> differences)
        {
            // If either is null, consider them equal
            if (valueV1 == null || valueV2 == null)
                return true;

            // Both are not null, do standard comparison
            if (!Equals(valueV1, valueV2))
            {
                differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Compares DateTime values with a specified tolerance.
    /// Allows flexible date/time comparison by accepting differences within the tolerance range.
    /// </summary>
    public class DateTimePrecisionComparer : ICustomComparer
    {
        private readonly TimeSpan _tolerance;

        /// <summary>
        /// Initializes a new instance with exact comparison (zero tolerance).
        /// </summary>
        public DateTimePrecisionComparer() : this(TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance with specified tolerance.
        /// </summary>
        /// <param name="toleranceSeconds">Tolerance in seconds. DateTime values within this range are considered equal.</param>
        public DateTimePrecisionComparer(double toleranceSeconds)
        {
            _tolerance = TimeSpan.FromSeconds(toleranceSeconds);
        }

        /// <summary>
        /// Initializes a new instance with specified tolerance TimeSpan.
        /// </summary>
        /// <param name="tolerance">Tolerance TimeSpan. DateTime values within this range are considered equal.</param>
        public DateTimePrecisionComparer(TimeSpan tolerance)
        {
            _tolerance = tolerance;
        }

        public bool Compare(object? valueV1, object? valueV2, string path, List<Difference> differences)
        {
            // Handle null cases
            if (valueV1 == null && valueV2 == null)
                return true;

            if (valueV1 == null || valueV2 == null)
            {
                differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            // Try to parse as DateTime
            DateTime dt1, dt2;
            
            if (valueV1 is DateTime dateTime1)
                dt1 = dateTime1;
            else if (DateTime.TryParse(valueV1.ToString(), out dt1) == false)
            {
                differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            if (valueV2 is DateTime dateTime2)
                dt2 = dateTime2;
            else if (DateTime.TryParse(valueV2.ToString(), out dt2) == false)
            {
                differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            // Calculate the absolute difference
            TimeSpan difference = dt1 > dt2 ? dt1 - dt2 : dt2 - dt1;

            // Compare with tolerance
            if (difference > _tolerance)
            {
                differences.Add(new Difference(path, dt1, dt2, DifferenceType.ValueMismatch));
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Serializes objects to JSON and compares them.
    /// Useful for comparing JObject, JToken, or other complex object types.
    /// Uses JSON serialization for accurate structural comparison.
    /// </summary>
    public class ObjectToStringComparer : ICustomComparer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectToStringComparer"/> class.
        /// </summary>
        public ObjectToStringComparer()
        {
        }

        public bool Compare(object? valueV1, object? valueV2, string path, List<Difference> differences)
        {
            System.Diagnostics.Debug.WriteLine($"📝 ObjectToStringComparer.Compare called for path: {path}");
            
            // Handle null cases
            if (valueV1 == null && valueV2 == null)
            {
                System.Diagnostics.Debug.WriteLine($"   Both values are null - Equal");
                return true;
            }

            if (valueV1 == null || valueV2 == null)
            {
                System.Diagnostics.Debug.WriteLine($"   One value is null - Not Equal");
                differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            // Serialize to JSON strings for accurate comparison
            string json1 = SerializeToJson(valueV1);
            string json2 = SerializeToJson(valueV2);
            
            System.Diagnostics.Debug.WriteLine($"   V1 Type: {valueV1.GetType().Name}, JSON: {json1.Substring(0, Math.Min(100, json1.Length))}...");
            System.Diagnostics.Debug.WriteLine($"   V2 Type: {valueV2.GetType().Name}, JSON: {json2.Substring(0, Math.Min(100, json2.Length))}...");

            // Compare JSON strings (ordinal comparison)
            if (json1 != json2)
            {
                System.Diagnostics.Debug.WriteLine($"   JSON strings do NOT match - Not Equal");
                differences.Add(new Difference(path, json1, json2, DifferenceType.ValueMismatch));
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"   JSON strings match - Equal");
            return true;
        }

        private string SerializeToJson(object value)
        {
            if (value == null)
                return "null";

            try
            {
                // Serialize to JSON using Newtonsoft.Json
                var json = JsonConvert.SerializeObject(value, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include,
                    Formatting = Formatting.None,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                return json;
            }
            catch (Exception ex)
            {
                // If serialization fails, fall back to ToString()
                System.Diagnostics.Debug.WriteLine($"   ⚠️ JSON serialization failed: {ex.Message}, falling back to ToString()");
                return value.ToString() ?? string.Empty;
            }
        }
    }
}
