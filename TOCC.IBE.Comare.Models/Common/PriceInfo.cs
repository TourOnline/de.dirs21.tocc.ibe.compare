using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOCC.IBE.Compare.Models.Common
{
    public class PriceInfoType
    {
        public Guid Product_uuid { set; get; }
        public ProductPriceCalculationModes? CalculationMode { set; get; }
        public PriceInfoTypeOffer Offer { get; set; }
        public decimal? PerPerson { set; get; }
        public decimal PerTick { set; get; }
        public decimal Total { set; get; }
        public PriceInfoTypeAfterDiscount AfterDiscount { get; set; }
    }

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
