using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOCC.IBE.Compare.Models.Common;
using TOCC.IBE.Compare.Models.Attributes;

namespace TOCC.IBE.Compare.Models.V1
{
    public class Set : BaseResultItem
    {
        public IList<object> Assigned { get; set; }
        public string Key { set; get; }

        [UniqueIdentifier("_uuid")]
        public List<ProductResult> Products { set; get; }
        public int? AssignedPersons { get; set; }
    }

    public class ProductResult : BaseResultItem
    {
        public Guid _uuid { set; get; }
        public IList<object> Assigned { get; set; }
        public int? Capacity { get; set; }
        public int? Count { get; set; }
        public ProductFacts Facts { set; get; }
        public bool IsDependency { set; get; }
        public string Name { get; set; }
        public ProductResultProductInfo ProductInfo { set; get; }

        [UniqueIdentifier("From")]
        public List<Tick> Ticks { set; get; }
        public virtual AvailabilityProductTypes Type => AvailabilityProductTypes.SingleProduct;

        public class Tick : BaseResultItem
        {
            public DateTime From { get; set; }
            public int Los { set; get; }

            public List<BaseOffer> Offers { get; set; }

            public DateTime Until { get; set; }
        }

        public class ProductResultProductInfo
        {
            public Guid _uuid { set; get; }
            public long _id { set; get; }
            public string _LegacyId { set; get; }
        }

        public class ProductFacts
        {
            public bool? AreTicksSelectable { set; get; }
            public bool? IsIncluded { set; get; }
            public bool? IsLimitedByBookedPersons { set; get; }
            public bool? IsMandatory { set; get; }
            public bool? IsOccupancySelectable { set; get; }
            public bool? IsOnRequest { set; get; }

        }

        public class ProductResultSelection
        {
            public int? MaxCount { set; get; }
            public ProductResultSelectionOccupancy Occupancy { get; set; }

            public bool ShouldSerializeOccupancy()
            {
                return Occupancy != null;
            }

            public class ProductResultSelectionOccupancy
            {
                public IList<string> Allowed { set; get; }
            }
        }
    }
}
