using System.Xml;

namespace Svenkle.SitecoreSolrOnStartup.Models
{
    public class Status : IStatus
    {
        public Status(string document)
        {
            Document = new XmlDocument();
            Document.LoadXml(document);
        }

        public bool IsSuccess => Document.SelectSingleNode("/response/lst[@name='error']") == null;
        public string Message => Document.SelectSingleNode("/response/lst[@name='error']/str[@name='msg']")?.InnerXml;
        public XmlDocument Document { get; }
    }
}