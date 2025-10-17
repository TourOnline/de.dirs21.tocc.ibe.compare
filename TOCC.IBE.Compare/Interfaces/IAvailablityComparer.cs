using TOCC.Contracts.IBE.Models.Availability;
using TOCC.IBE.Compare.Models.V1;

namespace TOCC.IBE.Compare.Interfaces
{
    public interface IAvailabilityComparer
    {
        bool Compare(V1Response responseV1, Response responseV2);
    }
}
