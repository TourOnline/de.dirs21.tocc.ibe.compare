using System;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace TOCC.IBE.Compare.Server.Infrastructure
{
    /// <summary>
    /// Custom serialization binder that handles missing assemblies gracefully during deserialization.
    /// This allows JSON deserialization to work even when some type dependencies are missing.
    /// </summary>
    public class SafeSerializationBinder : ISerializationBinder
    {
        private readonly DefaultSerializationBinder _defaultBinder = new DefaultSerializationBinder();

        public Type BindToType(string? assemblyName, string typeName)
        {
            try
            {
                // Try to bind to the type normally
                return _defaultBinder.BindToType(assemblyName, typeName);
            }
            catch (Exception)
            {
                // If the type can't be loaded due to missing assemblies, return null
                // This will cause JSON.NET to skip this property
                return null!;
            }
        }

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            _defaultBinder.BindToName(serializedType, out assemblyName, out typeName);
        }
    }
}
