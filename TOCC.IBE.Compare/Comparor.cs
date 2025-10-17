using TOCC.Contracts.IBE.Models.Availability;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TOCC.IBE.Compare.Interfaces;
using TOCC.IBE.Compare.Models.V1;
using TOCC.IBE.Compare.Models.Common;

namespace TOCC.IBE.Compare
{

    public class AvailabilityComparer : IAvailabilityComparer
    {
        private readonly TOCC.IBE.Compare.ComparisonRules _rules;

        public AvailabilityComparer(TOCC.IBE.Compare.ComparisonRules rules)
        {
            _rules = rules;
            Differences = new List<Difference>();
        }

        // visited pairs to avoid infinite recursion on cyclic object graphs
        // Map left-object -> (right-object -> firstSeenPath)
        private readonly Dictionary<object, Dictionary<object, string>> _visited = new(new ReferenceEqualityComparer());

        // Differences found during last comparison. Expected = V1, Actual = V2
        public List<Difference> Differences { get; }

        public bool Compare(V1Response responseV1, Response responseV2)
        {
            Differences.Clear();
            _visited.Clear();
            CompareObjects(responseV1, responseV2, responseV1?.GetType(), responseV2?.GetType(), "", 0);
            return Differences.Count == 0;
        }

        private const int MaxDepth = 300;

        private bool CompareObjects(object objV1, object objV2, Type typeV1, Type typeV2, string path, int depth)
        {
            if (depth > MaxDepth)
            {
                Differences.Add(new Difference(path + ".Depth", $"MaxDepth>{MaxDepth}", null, DifferenceType.Depth));
                return false;
            }

            if (objV1 == null && objV2 == null)
                return true;
            if (objV1 == null || objV2 == null)
            {
                Differences.Add(new Difference(path, objV1, objV2, DifferenceType.ValueMismatch));
                return false;
            }

            // detect cycles by tracking visited object pairs by reference
            //if (_visited.TryGetValue(objV1, out var inner))
            //{
            //    if (inner.TryGetValue(objV2, out var firstPath))
            //    {
            //        // record cycle occurrence for diagnostics: show where pair was first encountered
            //        Differences.Add(new Difference(path + ".Cycle", $"FirstSeen={firstPath}", $"Now={path}", DifferenceType.Cycle));
            //        return true;
            //    }
            //}
            //else
            //{
            //    inner = new Dictionary<object, string>(new ReferenceEqualityComparer());
            //    _visited[objV1] = inner;
            //}
            //inner[objV2] = path;

            var propsV1 = typeV1.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var propsV2 = typeV2.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var propV1 in propsV1)
            {
                // TO DO :  should be false if the property is missing in v2
                var propV2 = propsV2.FirstOrDefault(p => p.Name == propV1.Name);
                if (propV2 == null)
                    continue; // ignore missing property in V2

                // Skip validation if property is marked with SkipValidationAttribute v1
                var skipOnV1 = propV1.IsDefined(typeof(SkipValidationAttribute), true);
                if (skipOnV1)
                    continue;

                var currentPath = string.IsNullOrEmpty(path) ? propV1.Name : path + "." + propV1.Name;

                var valueV1 = propV1.GetValue(objV1);
                var valueV2 = propV2.GetValue(objV2);

                var comparer = _rules.GetComparer(propV2.PropertyType, propV2.Name);
                if (comparer != null)
                {
                    if (!comparer.Compare(valueV1, valueV2))
                        Differences.Add(new Difference(currentPath, valueV1, valueV2, DifferenceType.ValueMismatch));
                }
                else
                {
                    CompareValues(valueV1, valueV2, currentPath, depth + 1);
                }
            }

            return Differences.Count == 0;
        }

        private bool CompareValues(object valueV1, object valueV2, string path, int depth)
        {
            if (valueV1 == null && valueV2 == null)
                return true;
            if (valueV1 == null || valueV2 == null)
            {
                Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                return false;
            }

            // Compare collections (index-sensitive)
            // TO DO : index should be ignored if the property is marked with IgnoreIndexAttribute
            if (valueV1 is System.Collections.IEnumerable enumV1 && valueV2 is System.Collections.IEnumerable enumV2 && !(valueV1 is string))
            {
                var listV1 = enumV1.Cast<object>().ToList();
                var listV2 = enumV2.Cast<object>().ToList();
                if (listV1.Count != listV2.Count)
                {
                    Differences.Add(new Difference(path + ".Count", listV1.Count, listV2.Count, DifferenceType.Count));
                    return false;
                }

                for (int i = 0; i < listV1.Count; i++)
                {
                    var itemPath = path + "[" + i + "]";
                    var a = listV1[i];
                    var b = listV2[i];
                    if (_visited.TryGetValue(a, out var inner2))
                    {
                        if (inner2.ContainsKey(b))
                            continue;
                    }
                    else
                    {
                        inner2 = new Dictionary<object, string>(new ReferenceEqualityComparer());
                        _visited[a] = inner2;
                    }
                    inner2[b] = itemPath;
                    CompareValues(a, b, itemPath, depth + 1);
                }
                return true;
            }

            // Compare primitive types and strings
            var type1 = valueV1.GetType();
            var type2 = valueV2.GetType();

            // If one side is string and the other is enum, compare string to enum.ToString()
            if (type1 == typeof(string) && type2.IsEnum)
            {
                if (!string.Equals((string)valueV1, valueV2.ToString(), StringComparison.Ordinal))
                {
                    Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                    return false;
                }
                return true;
            }

            if (type2 == typeof(string) && type1.IsEnum) 
            {
                if (!string.Equals(valueV1.ToString(), (string)valueV2, StringComparison.Ordinal))
                {
                    Differences.Add(new Difference(path, valueV1, valueV2, DifferenceType.ValueMismatch));
                    return false;
                }
                return true;
            }

            if (type1.IsPrimitive || valueV1 is string || valueV1 is decimal || valueV1 is Guid)
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
    }

    public class Difference
    {
        public string Path { get; }
        public object? Expected { get; }
        public object? Actual { get; }

        public DifferenceType Type { get; set; } = DifferenceType.ValueMismatch;
        public Difference(string path, object? expected, object? actual, DifferenceType type)
        {
            Path = path;
            Expected = expected;
            Actual = actual;
            Type = type;
        }
    }

    public enum DifferenceType
    {
        ValueMismatch,
        MissingInV1,
        MissingInV2,
        Cycle,
        Depth,
        Count
    }

    public class Class1
    {
        private string? _message;

        public void AddMessage(string message)
        {
            _message = message;
        }

        public string? Message => _message;
    }
}
