using System;
using System.Collections.Generic;
using System.Reflection;

namespace TOCC.IBE.Compare
{
    public interface IPropertyComparer
    {
        bool Compare(object value1, object value2);
    }

    public class ComparisonRule
    {
        public Type PropertyType { get; set; }
        public string PropertyName { get; set; } // Optional: can be null for type-wide
        public IPropertyComparer Comparer { get; set; }
    }

    public class ComparisonRules
    {
        private readonly List<ComparisonRule> _rules = new();

        public void AddRule(Type propertyType, IPropertyComparer comparer, string propertyName = null)
        {
            _rules.Add(new ComparisonRule { PropertyType = propertyType, Comparer = comparer, PropertyName = propertyName });
        }

        public IPropertyComparer GetComparer(Type propertyType, string propertyName)
        {
            // First, try to match both type and property name
            var rule = _rules.Find(r => r.PropertyType == propertyType && r.PropertyName == propertyName);
            if (rule != null) return rule.Comparer;
            // Then, try to match type only
            rule = _rules.Find(r => r.PropertyType == propertyType && r.PropertyName == null);
            return rule?.Comparer;
        }
    }
}
