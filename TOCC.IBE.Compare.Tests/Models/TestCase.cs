using System;
using System.Collections.Generic;

namespace TOCC.IBE.Compare.Tests.Models
{
    /// <summary>
    /// Root test data structure containing properties and query parameter configurations.
    /// </summary>
    public class TestData
    {
        /// <summary>
        /// List of properties to test.
        /// </summary>
        public List<PropertyInfo> Properties { get; set; }

        /// <summary>
        /// List of query parameter configurations to test.
        /// Each configuration will be tested against all properties.
        /// </summary>
        public List<QueryConfig> QueryConfigurations { get; set; }
    }

    /// <summary>
    /// Represents a property to test.
    /// </summary>
    public class PropertyInfo
    {
        /// <summary>
        /// Property OID.
        /// </summary>
        public string _oid { get; set; }

        /// <summary>
        /// Channel UUID.
        /// </summary>
        public string _uuid { get; set; }

        /// <summary>
        /// Optional description for this property.
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents a query parameter configuration.
    /// </summary>
    public class QueryConfig
    {
        /// <summary>
        /// Name/description of this query configuration.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Query parameters for this configuration.
        /// </summary>
        public QueryParametersDto Parameters { get; set; }

        /// <summary>
        /// If true, this query configuration will be skipped during test execution.
        /// </summary>
        public bool Disabled { get; set; } = false;
    }

    /// <summary>
    /// Represents a single test case (property + query config combination).
    /// </summary>
    public class TestCase
    {
        public string Oid { get; set; }
        public string Uuid { get; set; }
        public string PropertyDescription { get; set; }
        public string QueryConfigName { get; set; }
        public QueryParametersDto QueryParameters { get; set; }
    }
}
