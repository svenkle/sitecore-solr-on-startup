using System.Net.Http;
using Svenkle.SitecoreSolrOnStartup.Models;

namespace Svenkle.SitecoreSolrOnStartup.Creators
{
    public interface ICreateSolrCore
    {
        bool CanCreate(ISystemInformation system, ICoreInformation core, string uri, string configuration);

        void Create(HttpClient httpClient, ISystemInformation system, ICoreInformation core, string uri, 
            string configuration, string coreName);
    }
}