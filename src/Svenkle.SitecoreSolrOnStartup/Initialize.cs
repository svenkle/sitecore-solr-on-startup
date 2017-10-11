using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using Sitecore.Configuration;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Svenkle.SitecoreSolrOnStartup.Creators;
using Svenkle.SitecoreSolrOnStartup.Models;

namespace Svenkle.SitecoreSolrOnStartup
{
    public class Initialize
    {
        protected readonly ICreateSolrCore[] Creators;

        public Initialize()
        {
            Creators = typeof(ICreateSolrCore).Assembly
                .GetTypes()
                .Where(x => typeof(ICreateSolrCore).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                .Select(x => (ICreateSolrCore)Activator.CreateInstance(x))
                .ToArray();
        }

        public void Process(PipelineArgs args)
        {
            if (!SolrContentSearchManager.IsEnabled)
                return;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    // At the time of writing Sitecore SOLR url cannot end with /. If they change that
                    // the assumption of no / would be wrong. To be sure, trim the end /.
                    var uri = Settings.GetSetting("ContentSearch.Solr.ServiceBaseAddress").TrimEnd('/');
                    var configuration = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data", "Solr");

                    var systemInformation = GetSystemInformation(httpClient, uri);
                    var coreInformation = GetCoreInformation(httpClient, systemInformation, uri);

                    var templatePath = Path.Combine(configuration, systemInformation.Version);
                    var schemaPath = Path.Combine(templatePath, "schema.xml");
                    var configurationPath = Path.Combine(templatePath, "solrconfig.xml");

                    CreateSitecoreSolrSchema(schemaPath);
                    CreateSitecoreSolrConfiguration(configurationPath);

                    foreach (var coreName in SolrContentSearchManager.Cores)
                    {
                        try
                        {
                            var processed = false;

                            foreach (var creator in Creators)
                            {
                                if (!creator.CanCreate(systemInformation, coreInformation, uri, configuration))
                                    continue;
                                
                                creator.Create(httpClient, systemInformation, coreInformation, uri, configuration, coreName);
                                processed = true;
                            }

                            if (!processed)
                                Log.Warn($"No SOLR creator processes ran for core {coreName}", this);
                        }
                        catch (Exception exception)
                        {
                            Log.Warn($"An error occurred while trying to create SOLR core {coreName}", exception);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Log.Error("An unhandled error occurred while trying to create SOLR cores", exception);
                }
            }
        }

        protected ISystemInformation GetSystemInformation(HttpClient httpClient, string endpointUri)
        {
            var xml = HttpClientHelper.GetXmlString(httpClient, $"{endpointUri}/admin/info/system");
            return new SystemInformation(xml);
        }

        protected ICoreInformation GetCoreInformation(HttpClient httpClient, ISystemInformation systemInformation, string endpointUri)
        {
            var xml = HttpClientHelper.GetXmlString(httpClient, systemInformation.Mode == Mode.SolrCloud ?
                $"{endpointUri}/admin/collections?action=LIST" : $"{endpointUri}/admin/cores?action=STATUS&indexInfo=false");

            return new CoreInformation(xml);
        }

        protected void CreateSitecoreSolrSchema(string schemaPath)
        {
            var schemaGenerator = new SchemaGenerator();
            schemaGenerator.GenerateSchema(schemaPath);
        }

        protected void CreateSitecoreSolrConfiguration(string solrConfigurationPath)
        {
            var configurationGenerator = new ConfigurationGenerator();
            configurationGenerator.GenerateConfiguration(solrConfigurationPath);
        }
    }
}