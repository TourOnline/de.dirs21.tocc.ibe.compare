using TOCC.IBE.Compare.Models.Common;

namespace TOCC.IBE.Compare.Models.Common
{
    /// <summary>
    /// Interface for building API call envelopes from test case parameters.
    /// </summary>
    public interface IApiCallEnvelopeBuilder
    {
        /// <summary>
        /// Builds API call envelope from test case parameters.
        /// </summary>
        ApiCallEnvelope BuildEnvelope(string channelUuid, TestCaseParameters parameters);
    }
}
