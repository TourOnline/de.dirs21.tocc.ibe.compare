using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TOCC.Contracts.IBE.Models.Availability;
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
    /// Handles mapping between V1 and V2 price structures:
    /// - Price.PerTick (V1) = Price.PerTick (V2)
    /// - Price.Total (V1) = Price.Total (V2)
    /// - Price.AfterDiscount.Total (V1) = Price.AfterDiscount.AfterTax (V2)
    /// </summary>
    public class PriceComparer : ICustomComparer
    {
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

            // Cast to V1 and V2 Price types
            var priceV1 = valueV1 as TOCC.IBE.Compare.Models.V1.PriceInfoType;
            var priceV2 = valueV2 as Price;

            if (priceV1 == null || priceV2 == null)
            {
                // Fallback to default comparison if types don't match
                return true;
            }

            bool isEqual = true;

            // Compare PerTick
            if (priceV1.PerTick != priceV2.PerTick)
            {
                differences.Add(new Difference($"{path}.PerTick", priceV1.PerTick, priceV2.PerTick, DifferenceType.ValueMismatch));
                isEqual = false;
            }

            // Compare Total
            if (priceV1.Total != priceV2.Total)
            {
                differences.Add(new Difference($"{path}.Total", priceV1.Total, priceV2.Total, DifferenceType.ValueMismatch));
                isEqual = false;
            }

            // Compare AfterDiscount
            if (priceV1.AfterDiscount != null && priceV2.AfterDiscount != null)
            {
                // CUSTOM MAPPING: V1.AfterDiscount.Total should equal V2.AfterDiscount.AfterTax
                if (priceV1.AfterDiscount.Total != priceV2.AfterDiscount.AfterTax)
                {
                    differences.Add(new Difference($"{path}.AfterDiscount.Total",
                        priceV1.AfterDiscount.Total,
                        priceV2.AfterDiscount.AfterTax,
                        DifferenceType.ValueMismatch));
                    isEqual = false;
                }
            }
            else if (priceV1.AfterDiscount != null || priceV2.AfterDiscount != null)
            {
                // One has AfterDiscount, the other doesn't
                differences.Add(new Difference($"{path}.AfterDiscount",
                    priceV1.AfterDiscount != null ? "<exists>" : "<missing>",
                    priceV2.AfterDiscount != null ? "<exists>" : "<missing>",
                    DifferenceType.ValueMismatch));
                isEqual = false;
            }

            return isEqual;
        }
    }

    /// <summary>
    /// Compares List of OfferTick objects with custom logic.
    /// Compares each V1.OfferTick.PersonPrices count with V2.OfferTick.Items.Count
    /// </summary>
    public class OfferTickComparer : ICustomComparer
    {
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

            // Cast to List of OfferTicks
            var ticksV1 = valueV1 as List<TOCC.IBE.Compare.Models.V1.OfferTicks>;
            var ticksV2 = valueV2 as List<TOCC.Contracts.IBE.Models.Availability.Offers.OfferTick>;

            if (ticksV1 == null || ticksV2 == null)
            {
                // Fallback to default comparison if types don't match
                return true;
            }

            bool isEqual = true;

            // Create lookup dictionary by composite key (Tariff_uuid, From) for V2
            var ticksV2Lookup = ticksV2.ToDictionary(t => (t.Tariff_uuid, t.From), t => t);

            // Compare each tick in V1 by matching Tariff_uuid
            foreach (var tickV1 in ticksV1)
            {
                if (tickV1 == null)
                    continue;

                // Find matching tick in V2 by composite key (Tariff_uuid, From)
                var compositeKey = (tickV1.Tariff_uuid, tickV1.From);
                if (!ticksV2Lookup.TryGetValue(compositeKey, out var tickV2))
                {
                    differences.Add(new Difference($"{path}[Tariff_uuid={tickV1.Tariff_uuid}, From={tickV1.From:yyyy-MM-dd HH:mm:ss}]", 
                        "<exists>", 
                        "<missing>", 
                        DifferenceType.MissingInV2));
                    isEqual = false;
                    continue;
                }

                if (tickV2 == null)
                {
                    differences.Add(new Difference($"{path}[Tariff_uuid={tickV1.Tariff_uuid}, From={tickV1.From:yyyy-MM-dd HH:mm:ss}]", 
                        tickV1, 
                        null, 
                        DifferenceType.ValueMismatch));
                    isEqual = false;
                    continue;
                }

                var tickPath = $"{path}[Tariff_uuid={tickV1.Tariff_uuid}, From={tickV1.From:yyyy-MM-dd HH:mm:ss}]";

                // Compare PersonPrices Count sum
                if (tickV1.PersonPrices.Sum(p => p.Count) != tickV2.PersonPrices?.Items.Sum(i => i.Count))
                {
                    differences.Add(new Difference($"{tickPath}.PersonPrices.Count",
                     tickV1.PersonPrices.Sum(p => p.Count),
                     tickV2.PersonPrices?.Items.Sum(i => i.Count),
                     DifferenceType.Count));
                    isEqual = false;
                }

                // Compare PersonPrices AfterDiscount.AfterTax sum with Items Total sum
                if (tickV1.PersonPrices.Sum(p => p.AfterDiscount?.AfterTax) != tickV2.PersonPrices?.Items.Sum(i => i.Total))
                {
                    differences.Add(new Difference($"{tickPath}.PersonPrices.AfterDiscount.AfterTax",
                     tickV1.PersonPrices.Sum(p => p.AfterDiscount?.AfterTax),
                     tickV2.PersonPrices?.Items.Sum(i => i.Total),
                     DifferenceType.ValueMismatch));
                    isEqual = false;
                }

                // Compare Price.Value with PersonPrices.Total.Total
                if (tickV1.Price?.Value != tickV2.PersonPrices?.Total?.Total)
                {
                    differences.Add(new Difference($"{tickPath}.Price.Value",
                       tickV1.Price?.Value,
                       tickV2.PersonPrices?.Total?.Total,
                       DifferenceType.ValueMismatch));
                    isEqual = false;
                }
            }

            // Check for items in V2 that don't exist in V1
            var ticksV1Lookup = ticksV1.Where(t => t != null).ToDictionary(t => (t.Tariff_uuid, t.From), t => t);
            foreach (var tickV2 in ticksV2)
            {
                if (tickV2 != null && !ticksV1Lookup.ContainsKey((tickV2.Tariff_uuid, tickV2.From)))
                {
                    differences.Add(new Difference($"{path}[Tariff_uuid={tickV2.Tariff_uuid}, From={tickV2.From:yyyy-MM-dd HH:mm:ss}]", 
                        "<missing>", 
                        "<exists>", 
                        DifferenceType.MissingInV1));
                    isEqual = false;
                }
            }

            return isEqual;
        }
    }

    /// <summary>
    /// Compares TOCC.IBE.Compare.Models.V1.Price objects with null check only.
    /// Skips comparison if either value is null.
    /// </summary>
    public class PropertyPriceComparer : ICustomComparer
    {
        public bool Compare(object? valueV1, object? valueV2, string path, List<Difference> differences)
        {
            // If either is null, skip comparison (consider them equal)
            if (valueV1 == null || valueV2 == null)
                return true;


            var priceV1 = valueV1 as TOCC.IBE.Compare.Models.V1.PriceInfo;
            var priceV2 = valueV2 as MinMaxPrice;

            bool isEqual = true;
            if (priceV1.Min?.PerTick != priceV2?.Min?.PerTick)
            {
                differences.Add(new Difference($"{path}.Min.PerTick",
                       priceV1.Min?.PerTick,
                       priceV2?.Min?.PerTick,
                       DifferenceType.ValueMismatch));
                isEqual= false;
            }


            if (priceV1.Min?.Total != priceV2?.Min?.Total)
            {
                differences.Add(new Difference($"{path}.Min.Total",
                                    priceV1.Min?.Total,
                                    priceV2?.Min?.Total,
                                    DifferenceType.ValueMismatch));
                isEqual = false;
            }

            // Both are not null, allow default comparison to proceed
            return isEqual;
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
            // Handle null cases
            if (valueV1 == null && valueV2 == null)
                return true;

            if (valueV1 == null || valueV2 == null)
            {
                differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            // Serialize to JSON strings for accurate comparison
            string json1 = SerializeToJson(valueV1);
            string json2 = SerializeToJson(valueV2);

            // Compare JSON strings (ordinal comparison)
            if (json1 != json2)
            {
                differences.Add(new Difference(path, json1, json2, DifferenceType.ValueMismatch));
                return false;
            }

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
            catch
            {
                // If serialization fails, fall back to ToString()
                return value.ToString() ?? string.Empty;
            }
        }
    }
}
