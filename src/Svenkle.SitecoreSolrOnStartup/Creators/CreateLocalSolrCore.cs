using System;
using System.IO;
using System.Net.Http;
using Sitecore.Diagnostics;
using Svenkle.SitecoreSolrOnStartup.Models;

namespace Svenkle.SitecoreSolrOnStartup.Creators
{
    public class CreateLocalSolrCore : ICreateSolrCore
    {
        public bool CanCreate(ISystemInformation system, ICoreInformation core, string uri, string configuration)
        {
            return system.Mode == Mode.Std;
        }

        public void Create(HttpClient httpClient, ISystemInformation system, ICoreInformation core, string uri,
            string configuration, string coreName)
        {
            if (core.HasCore(coreName))
                return;

            var configurationPath = Path.Combine(configuration, system.Version);
            var solrConfigurationPath = Path.Combine(system.Path, coreName, "conf");

            foreach (var dirPath in Directory.GetDirectories(configurationPath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(configurationPath, solrConfigurationPath));

            foreach (var newPath in Directory.GetFiles(configurationPath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(configurationPath, solrConfigurationPath), true);

            CreateCore(httpClient, uri, coreName);
        }

        private void CreateCore(HttpClient httpClient, string solrEndpointUri, string coreName)
        {
            try
            {
                var createCoreStatusXml = HttpClientHelper.GetXmlString(httpClient, $"{solrEndpointUri}/admin/cores?action=CREATE&name={coreName}");
                var status = new Status(createCoreStatusXml);

                if (status.IsSuccess)
                    Log.Info($"Created core {coreName}", this);
                else
                    Log.Error($"Unable to create SOLR core {coreName}. {status.Message}", this);
            }
            catch (Exception exception)
            {
                Log.Error($"Unable to create SOLR core {coreName}", exception, this);
            }
        }
    }
}