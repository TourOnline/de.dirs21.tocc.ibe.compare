using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOCC.IBE.Compare.Models.Common;
using TOCC.IBE.Compare.Models.Attributes;

namespace TOCC.IBE.Compare.Models.V1
{
    public class PersonPrice : Price
    {
        public int? Age { set; get; }
        public int Count { set; get; } = 1;
        public int Slot { set; get; }
    }
    public class PriceObsolet
    {
        /// <summary>
        /// Price per unit/person
        /// </summary>
        public decimal? BaseValue { set; get; }

        /// <summary>
        /// Price
        /// </summary>
        public decimal? Value { get; set; }

        public ValueTypes ValueType { get; set; }
    }

    public class Price
    {
        public PriceAfterDiscount AfterDiscount { set; get; }
        public PriceBeforeDiscount BeforeDiscount { set; get; }

        public class PriceAfterDiscount : PriceBeforeDiscount
        {
            public List<PriceDiscount> Discounts { set; get; }


        }

        public class PriceBeforeDiscount : PriceBase
        {
            public List<PriceSurcharge> Surcharges { set; get; }

        }

        public abstract class PriceBase
        {
            public decimal AfterTax { set; get; }
            public decimal BeforeTax { set; get; }

            public List<PriceTax> Taxes { set; get; }


        }

        public class PriceDiscount
        {
            public PriceDiscountSource Source { set; get; }
            public decimal Value { set; get; }



            public class PriceDiscountSource
            {
                public PriceAdjustmentsSources Type { set; get; }
                public Guid? _uuid { set; get; }
            }
        }

        public class PriceSurcharge : PriceDiscount
        {

        }

        public class PriceTax
        {
            public bool IsInclusive { set; get; }
            public Guid? TaxGroup { set; get; }
            public decimal Value { set; get; }
            public IEnumerable<PriceTaxPolicy> Policies { set; get; }


            public class PriceTaxPolicy
            {
                public decimal Value { set; get; }
                public Guid _uuid { get; set; }
            }
        }

    }
}
