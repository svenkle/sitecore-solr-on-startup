using System.Xml;

namespace Svenkle.SitecoreSolrOnStartup.Models
{
    public class CoreInformation : ICoreInformation
    {
        public CoreInformation(string document)
        {
            Document = new XmlDocument();
            Document.LoadXml(document);
        }

        public bool HasCore(string name)
        {
            return Document.SelectSingleNode($"/response/lst[@name='status']/lst[contains(@name, '{name}')]") != null;
        }

        public XmlDocument Document { get; }
    }
}