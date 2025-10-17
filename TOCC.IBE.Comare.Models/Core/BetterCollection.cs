using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOCC.IBE.Compare.Models.Core
{
    public class BetterCollection<T> 
    {
        private IEnumerable<T> _values = null;

        public BetterCollection()
        {
            IsForAll = false;
        }

        public static BetterCollection<T> All
        {
            get { return new BetterCollection<T> { IsForAll = true }; }
        }

        public IList<T> Except { set; get; }

        public bool IsForAll { set; get; }

        public IList<T> Items { set; get; }
         
    }
}
