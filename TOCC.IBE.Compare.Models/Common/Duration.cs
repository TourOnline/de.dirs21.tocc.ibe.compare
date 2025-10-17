using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOCC.IBE.Compare.Models.Common
{
    public partial class Duration
    {
        public TimeUnits? Unit { get; set; }
        public int? Value { get; set; }

        public TimeSpan? AsTickLength() => TimeUtils.ToTimeSpan(Value, Unit);
    }
}