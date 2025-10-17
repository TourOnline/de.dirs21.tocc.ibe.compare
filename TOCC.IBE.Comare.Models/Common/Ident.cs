using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOCC.IBE.Compare.Models.Common
{
    public class Ident
    {
        public long _oid { set; get; }
        public long _id { set; get; }
        public Guid _uuid { set; get; }
        public string _RepositoryName { set; get; }
        public string _LegacyId { set; get; }
    }
}
