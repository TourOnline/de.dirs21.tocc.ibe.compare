using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOCC.IBE.Compare.Models.Common
{
    public class Tag
    {
        public string Key { get; set; }
        public TagType Type { get; set; }
        public string Value { get; set; }
        public string Icon { get; set; } = "star";
    }
}
