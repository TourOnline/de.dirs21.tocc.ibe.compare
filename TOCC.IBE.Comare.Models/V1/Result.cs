using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOCC.IBE.Compare.Models.V1
{
    public class Result
    {
        public List<Property> Properties { set; get; }
    }

    public class Property : BaseResultItem
    {
        public long? _oid { set; get; }
        public List<Period> Periods { set; get; }
        public string Directory { set; get; }
    }

    public partial class Period : BaseResultItem
    {
        public List<Discount> Discounts { set; get; }
        public DateTime From { set; get; }
        public List<Set> Sets { set; get; }
        public DateTime Until { set; get; }
    }

    public class Discount
    {
        public DiscountCondition Condition { set; get; }
        public bool IsIncluded { set; get; }
        public ValueTypes Type { set; get; }
        public double Value { set; get; }
    }

    public class DiscountCondition
    {
        public Guid? Product_uuid { set; get; }
        public int ProductCount { set; get; }
        public Guid? Tariff_uuid { set; get; }
    }
}
