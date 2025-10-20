using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOCC.IBE.Compare.Models.Common
{
    public class QueryParameters
    {
        public BuildLevels BuildLevel { set; get; } = BuildLevels.Primary;
 
        public virtual Guid? Channel_uuid { get; set; }
      
        public virtual List<string> FilterByLegacyProducts { set; get; }
 
        public virtual List<string> FilterByLegacyTariffs { set; get; }
 
        public virtual List<Guid> FilterByProducts { set; get; }
 
        public virtual List<Guid> FilterByTariffs { set; get; }
 
        public virtual FlexOccupancySearchModes? FlexOccupancySearchMode { get; set; }
 
        public virtual DateTime? From { set; get; }
  
        public virtual int? Los { get; set; }

        public virtual int MaxParallelSearches { set; get; } = 20;
 
        public virtual string[] Occupancy { set; get; }

        public virtual OutputModes OutputMode { set; get; } = OutputModes.Availability;

        public virtual OutputTargetTypes TargetType { set; get; } = OutputTargetTypes.SingleProperty;
   
        public virtual DateTime? Until { set; get; }

        public bool IsAlternative { set; get; } = false;

        public string Currency { set; get; }
        public virtual Guid? SessionId { set; get; }
    }
}
