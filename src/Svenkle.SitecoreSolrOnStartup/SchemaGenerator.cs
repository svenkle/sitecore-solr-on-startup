using System;
using Sitecore.Diagnostics;

namespace Svenkle.SitecoreSolrOnStartup
{
    public class SchemaGenerator
    {
        public void GenerateSchema(string schemaPath)
        {
            try
            {
                var schemaGenerator = new Sitecore.ContentSearch.ProviderSupport.Solr.SchemaGenerator();
                schemaGenerator.GenerateSchema(schemaPath, schemaPath);
            }
            catch (Exception exception)
            {
                Log.Error($"An error occurred while adjusting {schemaPath}", exception, this);
            }
        }
    }
}
