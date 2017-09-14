using System;
using System.Xml;

namespace Svenkle.SitecoreSolrOnStartup.Models
{
    public class SystemInformation : ISystemInformation
    {
        public SystemInformation(string document)
        {
            Document = new XmlDocument();
            Document.LoadXml(document);
        }

        public string Version => Document.SelectSingleNode("/response/lst[@name='lucene']/str[@name='solr-spec-version']")?.InnerXml;

        public string Path => Document.SelectSingleNode("/response/str[@name='solr_home']")?.InnerXml;

        public Mode Mode => (Mode)Enum.Parse(typeof(Mode), Document.SelectSingleNode("/response/str[@name='mode']")?.InnerXml ?? "Std", true);

        public XmlDocument Document { get; }
    }
}