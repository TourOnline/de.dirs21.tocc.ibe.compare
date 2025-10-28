using System.Net.Http;

namespace TOCC.IBE.Compare.Tests.Helpers
{
    /// <summary>
    /// Test implementation of IHttpClientFactory for integration tests.
    /// </summary>
    public class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public TestHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            return _httpClient;
        }
    }
}
