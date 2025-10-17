using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOCC.IBE.Compare.Models.Common;
using TOCC.IBE.Compare.Models.Attributes;

namespace TOCC.IBE.Compare.Models.V1
{
    public class BaseResultItem
    {
        public ConstraintsInfo? Constraints { get; set; }
        public bool HasConstraints { set; get; }
        public bool IsAvailable { set; get; }
        public bool? IsValid { set; get; }
        public PriceInfo Price { set; get; }
        public List<Tag> Tags { set; get; }
    }
    public class ConstraintsInfo
    {
        public int? Capacity { set; get; }
        public bool? IsCta { get; set; }
        public bool? IsCtd { get; set; }
        public bool? IsOnlyCheckout { set; get; }
        public bool? IsUnfulfillable { set; get; }
        public int? MaxLos { get; set; }
        public int? MaxNumberOfPersons { set; get; }
        public int? MinLos { get; set; }
    }

    public class PriceInfo
    {
        public PriceInfoType Max { set; get; }
        public PriceInfoType Min { set; get; }
    }

    public class PriceInfoType
    {
        // Result.Properties[0].Periods[0].Sets[0].Products[0].Ticks[0].Offers[0].Price.Product_uuid
        [SkipValidation]
        public Guid Product_uuid { set; get; }

        [SkipValidation]
        public ProductPriceCalculationModes? CalculationMode { set; get; }
        public PriceInfoTypeOffer Offer { get; set; }
        public decimal? PerPerson { set; get; }
        public decimal PerTick { set; get; }
        public decimal Total { set; get; }
        public PriceInfoTypeAfterDiscount AfterDiscount { get; set; }

        public class PriceInfoTypeAfterDiscount
        {
            public decimal? PerPerson { set; get; }
            public decimal PerTick { set; get; }
            public decimal Total { set; get; }
        }

        public class PriceInfoTypeOffer
        {
            public Guid _uuid { get; set; }
        }
    }

}
