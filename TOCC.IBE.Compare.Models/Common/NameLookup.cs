using System;
using System.Collections.Generic;

namespace TOCC.IBE.Compare.Models.Common
{
    public static class NameLookup
    {
        private static IDictionary<Guid, string> _productNames = new Dictionary<Guid, string>();
        private static IDictionary<Guid, string> _tariffNames = new Dictionary<Guid, string>();

        public static void SetProducts(IDictionary<Guid, string> productNames)
        {
            _productNames = productNames ?? new Dictionary<Guid, string>();
        }

        public static void SetTariffs(IDictionary<Guid, string> tariffNames)
        {
            _tariffNames = tariffNames ?? new Dictionary<Guid, string>();
        }

        public static string GetProductName(Guid? uuid)
        {
            if (uuid.HasValue && _productNames.TryGetValue(uuid.Value, out var name) && !string.IsNullOrWhiteSpace(name))
                return name;
            return null;
        }

        public static string GetTariffName(Guid? uuid)
        {
            if (uuid.HasValue && _tariffNames.TryGetValue(uuid.Value, out var name) && !string.IsNullOrWhiteSpace(name))
                return name;
            return null;
        }

        public static void Clear()
        {
            _productNames = new Dictionary<Guid, string>();
            _tariffNames = new Dictionary<Guid, string>();
        }
    }
}
