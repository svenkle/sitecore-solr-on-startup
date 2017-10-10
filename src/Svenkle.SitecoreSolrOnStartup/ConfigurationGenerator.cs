using System;
using System.Text;
using System.Xml;
using Sitecore.Diagnostics;

namespace Svenkle.SitecoreSolrOnStartup
{
    public class ConfigurationGenerator
    {
        public void GenerateConfiguration(string solrConfigurationPath)
        {
            try
            {
                var document = new XmlDocument();
                document.Load(solrConfigurationPath);

                // Remove AddSchemaFieldsUpdateProcessorFactory
                var element = document.SelectSingleNode("/config/updateRequestProcessorChain/processor[@class='solr.AddSchemaFieldsUpdateProcessorFactory']");
                element?.ParentNode?.RemoveChild(element);

                // Enable ClassicIndexSchemaFactory
                var oldSchemaFactory = document.SelectSingleNode("/config/schemaFactory");
                var newSchemaFactory = document.CreateElement("schemaFactory");
                newSchemaFactory.SetAttribute("class", "ClassicIndexSchemaFactory");

                if (oldSchemaFactory == null)
                {
                    document.DocumentElement?.AppendChild(newSchemaFactory);
                }
                else
                {
                    document.DocumentElement?.ReplaceChild(newSchemaFactory, oldSchemaFactory);
                }

                using (var xmlWriter = new XmlTextWriter(solrConfigurationPath, Encoding.UTF8))
                {
                    document.WriteTo(xmlWriter);
                }
            }
            catch (Exception exception)
            {
                Log.Error($"An error occurred while adjusting {solrConfigurationPath}", exception, this);
            }
        }
    }
}