using System.Net.Http;
using System.Threading.Tasks;

namespace Svenkle.SitecoreSolrOnStartup
{
    public static class HttpClientHelper
    {
        public static string GetXmlString(HttpClient httpClient, string requestUri)
        {
            return Task.Run(() => httpClient.GetStringAsync(requestUri)).Result;
        }
    }
}
