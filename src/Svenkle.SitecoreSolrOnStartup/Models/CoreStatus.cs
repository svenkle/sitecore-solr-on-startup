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
            return Document.InnerText.Contains(name);
        }

        public XmlDocument Document { get; }
    }
}